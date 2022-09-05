﻿using System.Collections.Concurrent;

namespace Joba.IBM.RPA
{
    public class ParameterPullService
    {
        public ParameterPullService(IRpaClient client, Project project, Environment environment)
        {
            One = new PullOne(client, project, environment);
            Many = new PullMany(client, project, environment);
        }

        public IPullOne<Parameter> One { get; }
        public IPullMany Many { get; }

        class PullOne : IPullOne<Parameter>
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            internal PullOne(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinueOperationEventArgs<Parameter>>? ShouldContinueOperation;
            public event EventHandler<PulledOneEventArgs<Parameter>>? Pulled;

            public async Task PullAsync(string name, CancellationToken cancellation)
            {
                var local = environment.Dependencies.Parameters.Get(name);
                if (local == null)
                {
                    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
                    project.Dependencies.Parameters.Add(new NamePattern(parameter.Name));
                    environment.Dependencies.Parameters.AddOrUpdate(parameter);

                    Pulled?.Invoke(this, PulledOneEventArgs<Parameter>.Created(environment, project, parameter));
                }
                else
                {
                    var args = new ContinueOperationEventArgs<Parameter> { Resource = local, Project = project, Environment = environment };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
                    project.Dependencies.Parameters.Add(new NamePattern(parameter.Name));
                    environment.Dependencies.Parameters.AddOrUpdate(parameter);

                    PulledOneEventArgs<Parameter>? pulledArgs;
                    if (local.Value == parameter.Value)
                        pulledArgs = PulledOneEventArgs<Parameter>.NoChange(environment, project, local);
                    else
                        pulledArgs = PulledOneEventArgs<Parameter>.Updated(environment, project, parameter, local);
                    Pulled?.Invoke(this, pulledArgs);
                }
            }

            private async Task<Parameter> GetAndThrowIfDoesNotExistAsync(string name, CancellationToken cancellation)
            {
                var parameter = await client.Parameter.GetAsync(name, cancellation);
                if (parameter == null)
                    throw new Exception($"Could not find the parameter '{name}'");

                return parameter;
            }
        }

        class PullMany : IPullMany
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            internal PullMany(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PullingEventArgs>? Pulling;
            public event EventHandler<PulledAllEventArgs>? Pulled;

            public async Task PullAsync(CancellationToken cancellation)
            {
                var args = new ContinueOperationEventArgs { Project = project, Environment = environment };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue == false)
                    throw new OperationCanceledException("User cancelled the operation");

                Pulling?.Invoke(this, new PullingEventArgs { Project = project, Environment = environment });

                var fromWildcard = await PullWildcardAsync(cancellation);
                var fromFixed = await PullFixedAsync(cancellation);
                var parameters = fromWildcard.Concat(fromFixed).ToArray();
                environment.Dependencies.Parameters.AddOrUpdate(parameters);

                Pulled?.Invoke(this, new PulledAllEventArgs { Total = parameters.Length, Project = project, Environment = environment });
            }

            private async Task<IEnumerable<Parameter>> PullWildcardAsync(CancellationToken cancellation)
            {
                var wildcard = project.Dependencies.Parameters.GetWildcards();
                var tasks = wildcard
                    .Select(p => client.Parameter.SearchAsync(p.Name, 50, cancellation)
                        .ContinueWith(c => c.Result.Where(s => p.Matches(s.Name)), TaskContinuationOptions.OnlyOnRanToCompletion))
                    .ToList();

                var items = await Task.WhenAll(tasks);
                return items.SelectMany(p => p).ToList();
            }

            private async Task<IEnumerable<Parameter>> PullFixedAsync(CancellationToken cancellation)
            {
                var fixedParameters = project.Dependencies.Parameters.GetFixed();
                if (fixedParameters.Any())
                    return await client.Parameter.GetAsync(fixedParameters.ToArray(), cancellation);

                return Enumerable.Empty<Parameter>();
            }
        }
    }
}