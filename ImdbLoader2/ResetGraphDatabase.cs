using ImdbLoader2.Util;
using System;
using System.Threading.Tasks;

namespace ImdbLoader2
{
    public static class ResetGraphDatabase
    {
        public static async Task ExecuteAsync()
        {
            //dropping the entire collection is faster than trying to delete all the records
            var gh = await GraphHelper.CreateAsync();
            await gh.DropGraphAsync();
            gh = await GraphHelper.CreateAsync();
        }
    }
}