using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace Platform.ViewClients
{
    public class ViewClient
    {
        public readonly IViewContainer Advanced;

        readonly Func<Queue<Exception>, bool> _actionPolicy; 

        public ViewClient(IViewContainer advanced, Func<Queue<Exception>, bool> actionPolicy)
        {
            Advanced = advanced;
            _actionPolicy = actionPolicy;
        }

        public TEntity ReadAsJsonOrNull<TEntity>(string name) where TEntity : class
        {
            return GetResult(() =>
                {
                    if (!Advanced.Exists(name))
                        return null;
                    using (var stream = Advanced.OpenRead(name))
                    {
                        return JsonSerializer.DeserializeFromStream<TEntity>(stream);
                    }
                });
        }

        public TEntity ReadAsJsonOrGetNew<TEntity>(string name) where TEntity : class,new()
        {
            return ReadAsJsonOrNull<TEntity>(name) ?? new TEntity();
        }

        TResult GetResult<TResult>(Func<TResult> func)
        {
            Queue<Exception> errors = null;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    if (errors == null)
                    {
                        errors = new Queue<Exception>();
                    }
                    errors.Enqueue(ex);
                    if (_actionPolicy(errors))
                        throw new AggregateException(ex.Message, ex);
                }
            }
        }

        public void CreateContainer()
        {
            GetResult(() => Advanced.Create());
        }
        public bool Exists(string name)
        {
            return GetResult(() => Advanced.Exists(name));
        }
        public void WriteAsJson<TEntity>(TEntity entity, string name)
        {
            GetResult(() =>
                {
                    using (var stream = Advanced.OpenWrite(name))
                    {
                        JsonSerializer.SerializeToStream(entity, stream);
                    }
                    return 0;
                });
        }
    }
}