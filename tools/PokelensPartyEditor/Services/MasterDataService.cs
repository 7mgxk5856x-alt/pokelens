using PokelensCore;
using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary><see cref="MasterDataReader.Load"/> を起動時に 1 回呼び、結果をメモリに保持する。</summary>
public sealed class MasterDataService : IMasterDataService
{
    public MasterDataSnapshot Snapshot { get; }

    public MasterDataService()
    {
        Snapshot = MasterDataReader.Load();
    }
}
