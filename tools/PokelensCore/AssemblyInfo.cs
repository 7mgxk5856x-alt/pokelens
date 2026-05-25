using System.Runtime.CompilerServices;

// テスト側で internal 型・メソッドを参照するため公開する。
// 本番から見える API は public のみ。MasterDataReader の Load* 内部メソッドはテストでの細分検証のため internal 公開。
[assembly: InternalsVisibleTo("PokelensCore.Tests")]
