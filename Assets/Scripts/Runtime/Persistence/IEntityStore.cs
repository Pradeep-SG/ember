using System.Collections.Generic;

namespace Kindrith.Persistence
{
    public interface IEntityStore<T> where T : class
    {
        T Load(string id);
        void Save(string id, T entity);
        void Delete(string id);
        IEnumerable<string> ListIds();
    }
}
