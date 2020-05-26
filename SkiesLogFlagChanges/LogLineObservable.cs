using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SkiesLogFlagChanges
{
    public class LogLineObservable : IDisposable, IObservable<string>
    {
        private readonly FileStream stream;
        private readonly int millisecondsDelay;
        protected readonly StreamReader reader;
        protected readonly CancellationTokenSource cancellationTokenSource;
        private bool shouldSendCompletionNotifications = false;

        // It's safe to use the System.Collections.Generic list because we're only reading from
        // the list concurrently.  If we later mutate the list concurrently, we need to change
        // to a concurrent collection
        private readonly List<IObserver<string>> observers;

        public LogLineObservable(string path, int millisecondsDelay = 50)
        {
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            reader = new StreamReader(stream);
            this.millisecondsDelay = millisecondsDelay;
            cancellationTokenSource = new CancellationTokenSource();

            observers = new List<IObserver<string>>();
        }

        public virtual void StartListening()
        {
            Task.Run(() => ListeningTask(reader, observers, cancellationTokenSource.Token));
        }

        public virtual async void ListeningTask(StreamReader reader, List<IObserver<string>> observers, CancellationToken ct)
        {
            stream.Seek(0, SeekOrigin.End);

            while (!ct.IsCancellationRequested)
            {
                while(!reader.EndOfStream)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();
                        foreach (var observer in observers)
                        {
                            observer.OnNext(line);
                        }

                        await Task.Delay(millisecondsDelay, ct);
                    }
                    catch(TaskCanceledException)
                    {
                        // Ignore task cancelled exceptions as we are expecting those.
                        break;
                    }
                    catch (Exception ex)
                    {
                        foreach(var observer in observers)
                        {
                            observer.OnError(ex);
                        }
                    }
                }
            }

            if(shouldSendCompletionNotifications)
            {
                foreach(var observer in observers)
                {
                    observer.OnCompleted();
                }
            }
        }

        public virtual void StopListening(bool final = false)
        {
            this.shouldSendCompletionNotifications = final;
            cancellationTokenSource.Cancel();
        }

        #region IObservable Support
        public IDisposable Subscribe(IObserver<string> observer)
        {
            if(!this.observers.Contains(observer))
            {
                this.observers.Add(observer);
            }
            return new Unsubscriber(this.observers, observer);
        }

        private class Unsubscriber : IDisposable {
            private readonly List<IObserver<string>> allObservers;
            private readonly IObserver<string> observer;

            public Unsubscriber(List<IObserver<string>> allObservers, IObserver<string> thisObserver)
            {
                this.allObservers = allObservers;
                this.observer = thisObserver;
            }
            public void Dispose()
            {
                if(this.observer != null && this.allObservers.Contains(observer))
                {
                    this.allObservers.Remove(this.observer);
                }
            }
        }
        #endregion
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopListening(true);

                    reader?.Dispose();
                    stream?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
