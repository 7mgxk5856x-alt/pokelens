using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary>マスターデータスナップショットへのアクセスを抽象化する Service（テスト時モック）。</summary>
public interface IMasterDataService
{
    MasterDataSnapshot Snapshot { get; }
}
