using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary>data/party.json の読み書きを担う Service。ViewModel テスト時はモック差し替え。</summary>
public interface IPartyFileService
{
    PartyFileLoadResult Load();
    PartyFileSaveResult Save(PartyDocument document);
}
