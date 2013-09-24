CaveTalk(壁トーーク)
======================

Download
----------------
<http://sdrv.ms/X1wy8M>

License
-------

Copyright 2011-2013, まどがい  
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


WebSocket4Net  
<http://websocket4net.codeplex.com/>

Apache License, Version 2.0  
<http://www.apache.org/licenses/>


SocketIO4Net.Client  
<http://socketio4net.codeplex.com/>

MIT-style License.  
<http://www.opensource.org/licenses/mit-license.php>

概要
----

[CaveTube](http://gae.cavelis.net/)用のコメント読み上げソフトです。  

システム要件
------------

以下の環境が必要です。

* [.NET Framework 4.5](http://www.microsoft.com/ja-jp/download/details.aspx?id=30653)
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

更新履歴
--------
2013/09/24
* 昇順にソートしている場合にオートスクロールする機能を追加しました(4.5版のみ)。
* 棒読みちゃんへ渡すオプションを調整できるようにしました(4.5版のみ)。
* 配信URL以外を入力するとアプリケーションが終了してしまう問題を修正しました。

2013/08/01
* 配信開始APIのアップデートに対応しました。

2013/06/19
* コメントサーバのアップデートに対応しました。
* サムネイル選択に対応しました(4.5版のみ)。
* ジャンル選択に対応しました(4.5版のみ)。

2013/03/07
* コメントサーバのアップデートに対応しました。
* コメントの非表示機能に対応しました。
* Flashコメントジェネレーターへの出力に対応しました。
* Githubに繋がらないと起動できないバグを修正しました。

2012/12/01
* ログイン時に標準で固定URLを表示するようにしました。
* 配信終了通知を音声メッセージからダイアログに変更しました。
* 配信開始時にタグが反映されない問題を修正しました。
* コメント通知音を停止しないオプションを追加しました。
* アイコンを変更しました。
* 依存する.NET Frameworkのバージョンを4.5に変更しました。

2012/10/08
* コメントサーバの仕様変更に対応しました。以下の機能が再度使用できるようになりました。
	* 配信の開始
	* リスナーBAN
	* 強制ID表示
* ソフトウェア起動時にアップデートのチェックを行うようにしました。
* ウィンドウ位置とサイズを保存するようにしました。
* コメントの配信開始からの経過時間を表示するようにしました。
* ID、投稿時間、経過時間のカラムを非表示にできるようにしました。ヘッダーを右クリックすることで変更できます。
* ユーザーサウンドを使用している際にミキサーに複数アイコンが出てしまう問題に対応しました。
* ユーザーサウンド機能を強化し、再生秒数、再生音量を調整できるようにしました。
* 配信終了通知を受けて通知する機能を追加しました。
* 未接続時にタスクトレイに収まる機能を削除しました。

2012/06/23
* データベースをSQLiteに変更しました。
* WebSocketとSocketIOのライブラリをSocketIO4Netに変更しました。それに伴いWebSocketのバージョンもRFC 6455に変更されています。
* URLのプレビュー機能を追加しました。
* CaveTalkから配信が開始できるようにしました。
* 管理者メッセージを受け取れるようにしました。
* 読み上げソフトの代わりに好きなサウンドを鳴らせる機能を追加しました。
* 大幅なリファクタリングを行いました。

2012/03/04
* 組み込みデータベースを導入しました。内部にはSQLServerCompact4.0を使用しています。
* ID付きのコメントに色を付けられるようにしました。ダブルクリックでランダムに色を付けられます。
* URLをクリックしてブラウザを開くことができるようにしました。
* パスワードの入力が隠れるようになりました。
* フォントサイズを変更できるようにしました。
* 最前面表示オプションをつけました。オプションから設定してください。

2011/12/27
* IDを表示するようにしました。
* コメント番号と投稿者名の読み上げに対応しました。
* live/user形式のロードに対応しました。
* アスキーアートとAmazonへのリンクを省略するようにしました。
* AAを綺麗に表示するようにしました。

