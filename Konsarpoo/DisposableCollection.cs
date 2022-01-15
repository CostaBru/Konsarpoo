using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Default disposable collection interface.
    /// </summary>
    public interface IDisposableCollection
    {
        /// <summary>
        /// Adds new disposable to the list.
        /// </summary>
        /// <param name="disposable"></param>
        void AddDisposable(IDisposable disposable);

        /// <summary>
        /// Remove disposable from the list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        bool RemoveDisposable(IDisposable disposable);

        /// <summary>
        /// Returns the contents of container.
        /// </summary>
        IReadOnlyList<IDisposable> Items { get; }
    }
    
    
    /// <summary>
    /// Default disposable collection implementation.
    /// </summary>
    public class DisposableCollection : IDisposableCollection
    {
        private readonly Data<IDisposable> m_list = new ();
     
        public void AddDisposable([NotNull] IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }
            
            m_list.Add(disposable);
        }
       
        public bool RemoveDisposable([NotNull] IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }
            
            return m_list.Remove(disposable);
        }
       
        public IReadOnlyList<IDisposable> Items => m_list;
      
        public void Dispose()
        {
            foreach (var disposable in m_list)
            {
                disposable.Dispose();
            }

            m_list.Dispose();
        }
    }
}
