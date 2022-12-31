using System.Collections.Generic;
using System.Threading.Tasks;
using CommonLib.Web.Source.ViewModels;

namespace CommonLib.Web.Source.Services
{
    public class TestDataManager // not injectable for testing
    {
        private static List<TestDataVM> _data { get; } = new();

        public async Task CreateAsync(TestDataVM itemToInsert)
        {
            itemToInsert.Id = _data.Count + 1;
            _data.Insert(0, itemToInsert);
            await Task.CompletedTask;
        }

        public async Task<List<TestDataVM>> ReadAsync()
        {
            if (_data.Count < 1)
            {
                for (var i = 1; i < 50; i++)
                {
                    _data.Add(new TestDataVM
                    {
                        Id = i,
                        Name = "Name " + i
                    });
                }
            }

            return await Task.FromResult(_data);
        }

        public async Task UpdateAsync(TestDataVM itemToUpdate)
        {
            var index = _data.FindIndex(i => i.Id == itemToUpdate.Id);
            if (index != -1)
                _data[index] = itemToUpdate;
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(TestDataVM itemToDelete)
        {
            await Task.FromResult(_data.Remove(itemToDelete));
        }
    }
}
