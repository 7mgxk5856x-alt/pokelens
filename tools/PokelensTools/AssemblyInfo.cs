using System.Runtime.CompilerServices;

// テストアセンブリから internal な型・メンバーを検証できるようにする。
// development-guidelines「アクセス制御（カプセル化）」に従い、テストのために public 化せず
// InternalsVisibleTo で可視性を限定的に緩める。
[assembly: InternalsVisibleTo("PokelensTools.Tests")]
