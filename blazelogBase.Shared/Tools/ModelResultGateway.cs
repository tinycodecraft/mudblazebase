using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Tools
{
    class Unsubscriber<T> : IDisposable
    {
        private readonly List<IObserver<KeyValuePair<string, T>>> _observers;
        private readonly IObserver<KeyValuePair<string, T>> _observer;

        public Unsubscriber(List<IObserver<KeyValuePair<string, T>>> observers, IObserver<KeyValuePair<string, T>> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (!(_observer == null))
            {
                _observers.Remove(_observer);
            }
        }
    }
    public class ModelResultGateway<T>:IResultGateway<T>
    {
        private readonly List<IObserver<KeyValuePair<string,T>>> observers = new();
        private readonly ILogger<ModelResultGateway<T>> log;
        public ModelResultGateway(ILogger<ModelResultGateway<T>> logger ) {
            log = logger;
        }

        public Task OnDeliverResultAsync(KeyValuePair<string, T> result)
        {
            if(observers.Any())
            {
                foreach(var  observer in observers)
                {
                    observer.OnNext(result);
                    log.LogInformation($"deliver result against {result.Key}");
                }
            }

            return Task.CompletedTask;
        }

        

        public IDisposable Subscribe(IObserver<KeyValuePair<string, T>> observer)
        {

            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber<T>(observers, observer);
        }


    }
}
