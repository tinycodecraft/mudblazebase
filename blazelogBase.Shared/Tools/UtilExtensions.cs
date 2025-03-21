using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace blazelogBase.Shared.Tools
{
    public static class IdGenerator
    {
        private static int _counter;

        private static SemaphoreSlim _lock = new SemaphoreSlim(1);


        public static async void GetLock()
        {
            await _lock.WaitAsync();
        }

        public static void Release()
        {
            _lock.Release();
        }

        public static uint GetNewId()
        {
            uint newId = unchecked((uint)System.Threading.Interlocked.Increment(ref _counter));
            if (newId == 0)
            {
                _counter = 0;
                return GetNewId();
                //throw new System.Exception("Whoops, ran out of identifiers");
            }
            return newId;
        }
    }
    public static class UtilExtensions
    {

        public static string CreateXml<T>(T input) where T: class
        {
            var x = new XmlSerializer(typeof(T));

            using (StringWriter textWriter = new StringWriter())
            {
                x.Serialize(textWriter, input);

                return textWriter.ToString();
            }
        }

        /// <summary>
        /// Creates a byte array from the string, using the 
        /// System.Text.Encoding.Default encoding unless another is specified.
        /// </summary>
        /// <param name="str">The str<see cref="string"/>.</param>
        /// <param name="encoding">The encoding<see cref="Encoding"/>.</param>
        /// <returns>The <see cref="byte[]"/>.</returns>
        public static byte[] ToByteArray(string str, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.Default;
            return encoding.GetBytes(str);
        }


        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken = default(CancellationToken), int bufferSize = 0x1000)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                Thread.Sleep(10);
                progress.Report(totalRead);
            }
        }

        /// <summary>
        /// The RandomString.
        /// </summary>
        /// <param name="size">The size<see cref="int"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string RandomString(this string me, int size = 5)
        {
            StringBuilder sb = new StringBuilder();

            int myIntValue = unchecked((int)DateTime.Now.Ticks + me.GetHashCode());
            myIntValue = unchecked(myIntValue + (int)IdGenerator.GetNewId());
            var rnd = new Random(myIntValue);
            for (int i = 0; i < size; i++)
            {


                sb.Append(Convert.ToChar(rnd.Next(65, 90)));
            }

            return sb.ToString().ToLowerInvariant();
        }

        public static bool HasError(this string error)
        {
            return !string.IsNullOrWhiteSpace(error);
        }


        // all error checking left out for brevity

        // a.k.a., linked list style enumerator
        public static IEnumerable<TSource> FromHierarchy<TSource>(
            this TSource source,
            Func<TSource, TSource> nextItem,
            Func<TSource, bool> canContinue)
        {
            for (var current = source; canContinue(current); current = nextItem(current))
            {
                yield return current;
            }
        }

        public static IEnumerable<TSource> FromHierarchy<TSource>(
            this TSource source,
            Func<TSource, TSource> nextItem)
            where TSource : class
        {
            return FromHierarchy(source, nextItem, s => s != null);
        }

        public static IEnumerable<string> ToErrorMessageList(this Exception ex, bool allerror = false)
        {
            if (!allerror && (ex is ThreadAbortException || ex is OperationCanceledException))
            {
                return new[] { ex.Message };
            }
            else
                return ex.FromHierarchy(x => x.InnerException)
                    .Select(x => x.Message);
        }

        public static IEnumerable<KeyValuePair<string, string>> ToErrorList(this Exception ex)
        {
            if (ex is ThreadAbortException || ex is OperationCanceledException)
            {
                return new[] { new KeyValuePair<string, string>("Operation Cancelled", ex.Message) };
            }
            else
                return ex.FromHierarchy(x => x.InnerException)
                    .Select(x => new KeyValuePair<string, string>(x.Source, x.Message))
                    .Union(new[] { new KeyValuePair<string, string>("stacktrace", ex.StackTrace) });
        }
    }
}
