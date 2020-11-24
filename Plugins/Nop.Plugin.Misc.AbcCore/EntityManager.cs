using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcCore
{
    /// <summary>
    /// A helper class to manage bulk inserts and updates
    /// </summary>
    public class EntityManager<T> where T : BaseEntity
    {
        private IRepository<T> _repo;
        private readonly int _bufferSize;
        private IList<T> _insertBuffer;
        private IList<T> _updateBuffer;

        public EntityManager()
        {
            _repo = EngineContext.Current.Resolve<IRepository<T>>();
            _bufferSize = 500;
            _insertBuffer = new List<T>();
            _updateBuffer = new List<T>();
        }

        public EntityManager(IRepository<T> repo)
        {
            _repo = repo;
            _bufferSize = 500;
            _insertBuffer = new List<T>();
            _updateBuffer = new List<T>();

        }

        public void Insert(T entity)
        {
            _insertBuffer.Add(entity);

            if (_insertBuffer.Count >= _bufferSize)
            {
                _repo.Insert(_insertBuffer);
                _insertBuffer.Clear();
            }
        }

        public void Update(T entity)
        {
            _updateBuffer.Add(entity);

            if (_updateBuffer.Count >= _bufferSize)
            {
                _repo.Update(_updateBuffer);
                _updateBuffer.Clear();
            }
        }

        public void Flush()
        {
            _repo.Insert(_insertBuffer);
            _repo.Update(_updateBuffer);
            _insertBuffer.Clear();
            _updateBuffer.Clear();
        }
    }
}
