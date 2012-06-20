CaveTalk(壁トーーク)
======================

License
-------

Copyright 2011, まどがい  
MIT-style License.  
<http://www.opensource.org/licenses/mit-license.php>


ParallelExtensionsExtras  
Author: Microsoft Corporation  

Apache License, Version 2.0  
<http://www.apache.org/licenses/>


Connect Icon  
Author: David Vignoni  
<http://www.icon-king.com/>

LGPL License Version 3  
<http://www.gnu.org/copyleft/lesser.html>


概要
----

[CaveTube](http://gae.cavelis.net/)用のコメント読み上げソフトです。  
沢山改変して便利にしてくれると嬉しいです。  

システム要件
------------

以下の環境が必要です。

* [.NET Framework 4](http://www.microsoft.com/downloads/ja-jp/details.aspx?FamilyID=9cfb2d51-5ff4-4491-b0e5-b386f32c0992)
* [棒読みちゃん](http://chi.usamimi.info/Program/Application/BouyomiChan/)

※ 棒読みちゃんの代わりに[SofTalk](http://www35.atwiki.jp/softalk/)に読ませることも可能です。

使い方
------

放送URLにCaveTubeの視聴URL、または部屋IDを入力して接続ボタンを押すことで放送に接続できます。  
新しいコメントがサーバから送られてくると棒読みちゃんにリクエストを送信します。

CaveTalkを起動する前に棒読みちゃんを起動しておくと便利です。

CaveTubeの新規配信やコメントをポップアップ表示する機能などもオプションで設定できます。

既知の不都合
------------
一部のウィルス対策ソフトが入っていると動作しません。(AVG2012など)  
これはウィルス対策ソフトが、CaveTubeが使用しているWebSocketという通信方法を塞いでしまうためです。

今後の追加予定
--------------
* アップデートの監視
* したらば対応
* プラグイン化

PR
--

作者は以下のサイトを運営しています。よければご覧ください。

大会運営ツール ToNaMeT  
<http://www.tonamet.com>

更新履歴
--------

2011/12/27
* IDを表示するようにしました。
* コメント番号と投稿者名の読み上げに対応しました。
* live/user形式のロードに対応しました。
* アスキーアートとAmazonへのリンクを省略するようにしました。
* AAを綺麗に表示するようにしました。

2012/03/04
* 組み込みデータベースを導入しました。内部にはSQLServerCompact4.0を使用しています。
* ID付きのコメントに色を付けられるようにしました。ダブルクリックでランダムに色を付けられます。
* URLをクリックしてブラウザを開くことができるようにしました。
* パスワードの入力が隠れるようになりました。
* フォントサイズを変更できるようにしました。
* 最前面表示オプションをつけました。オプションから設定してください。

