#	コメント欄の読み上げの仕様書
2011/07/31

## はじめに
HTTP通信とWebSocket通信をつかったかべつべのコメント読み上げ周りお仕様書を書いておきます  
すごく適当でごめんなさい。

サンプルでは、配信のstream_nameが

	AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

とします。


## HTTPリクエスト
コメントを1番目から全部取得するには

	http://gae.cavelis.net/viewedit/getcomment2?stream_name=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA&comment_num=1

コメントの10番目のみ取得するには

	http://gae.cavelis.net/viewedit/getcomment2?stream_name=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA&comment_num=10&single=true

コメントの11番目以降を取得するには

	http://gae.cavelis.net/viewedit/getcomment2?stream_name=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA&comment_num=11&single=false

※リクエストはGETでもPOSTでもOK

## HTTPレスポンス
レスポンスは全てjsonで返信される。(文字コードはUTF-8)  
レスポンスにはコメント情報以外にも、返信の成否と、視聴者情報が含まれる

	{
		ret : true or false,	//返信が成功したかどうか (ret:falseの場合は、他のデータは存在しない)

		listener : 8,		//現在の視聴者数(★この情報は2分毎更新なのでリアルタイムの情報ではない)
		max_listener : 10,		//最大視聴者数	(★この情報は2分毎更新なのでリアルタイムの情報ではない)
		viewer : 25,		//ページビュー
		comment_num : 125,		//最新のコメント数

		comments: [
			{
				comment_num : 1,			//コメントの番号
				message : "これが1コメ目",		//生のコメント内容
				html : "<div>これが1コメ目</div>",	//HTML化されたコメント内容
				name : "hoge",				//★コメント投稿者の名前
				time : 19190721,			//★コメント投稿の日付 (単位はUnixTime 日本時間に直すにはタイムゾーン補正も必要)
				is_ban : false,			//★コメントがBANされているかどうか
				auth : true,				//★コメントが認証コメント(緑コテ状態)かどうか
			},
			{
				comment_num : 2,			//コメント番号
				message : "これが2コメ目",		//生のコメント内容
				html : "<div>これが2コメ目</div>",	//HTML化されたコメント内容
				name : "hoge",				//★コメント投稿者の名前
				time : 19190722,			//★コメント投稿の日付 (単位はUnixTime 日本時間に直すにはタイムゾーン補正も必要)
				is_ban : false,			//★コメントがBANされているかどうか
				auth : true,				//★コメントが認証コメント(緑コテ状態)かどうか
			}
		]
		num_1.comment_num : 1,			//コメントの番号
		num_1.message : "これが1コメ目",		//生のコメント内容
		num_1.html : "<div>これが1コメ目</div>",	//HTML化されたコメント内容
		num_1.name : "hoge",			//★コメント投稿者の名前
		num_1.time : 19190721,			//★コメント投稿の日付 (単位はUnixTime 日本時間に直すにはタイムゾーン補正も必要)
		num_1.is_ban : false,			//★コメントがBANされているかどうか
		num_1.auth : true,				//★コメントが認証コメント(緑コテ状態)かどうか

		num_2.comment_num : 2			//コメント番号
		num_2.message : "これが2コメ目",		//生のコメント内容
		num_2.html : "<div>これが2コメ目</div>",	//HTML化されたコメント内容
		num_2.name : "hoge",			//★コメント投稿者の名前
		num_2.time : 19190722,			//★コメント投稿の日付 (単位はUnixTime 日本時間に直すにはタイムゾーン補正も必要)
		num_2.is_ban : false,			//★コメントがBANされているかどうか
		num_2.auth : true,				//★コメントが認証コメント(緑コテ状態)かどうか

		(以下、コメントの数だけループ)
	}

### HTTPでの注意点
httpによるコメント取得はサーバー側でロングポーリング(新しいコメントが届くまでレスポンスを返さない処理)は行っていないです。  
なので、該当するコメントが存在しない場合は、即時にエラーの返信(ret:false)が返信されます。  
毎秒http通信されるとサーバーが過負荷になるので、コメントソフトでは、コメントは最初に全取得をしたのち、リアルタイムのコメント検出はWebSocket通信を使ってください。  

## WebSocketでの通信
* コメントのサーバーはnode.jsというHTTPアプリケーションサーバーをつかって、WebSocketライブラリとしてsocket.ioを使っています。
	* node.jsのバージョン 0.4.10
	* socket.ioのバージョン 0.7.8
* アドレスとポートは以下の通りです
	* ws.cavelis.net:3000

以下、socket.ioを使ったリアルタイムコメント取得について、javascriptでの実装を示します。  
サンプルで使ってるjavascriptライブラリは4つです。

* jqurey 1.6.1	//ご存知javascript界の救世主
* jquery.json	//json変換のjqueryプラグイン
* swfobject 2.2	//flashをロードするライブラリ (socket.ioがFlashPlayerによるWebSocket通信で使います)
* socket.io.js	//socket.ioのwebsocketスクリプト

## コード例

	<html lang="ja">
	<head>
	<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
	<meta http-equiv="Content-Script-Type" content="text/javascript">
	<title>WebSocketテスト</title>
	<script type="text/javascript" src="https://www.google.com/jsapi?key=ABQIAAAA-gyesKrmfs3l_jUINRA7BBSbZWALvqIfi5Wo0va4FJuOn1eqSBShg_FXOT1DSj2ZniTDIuFw9QMPiQ"></script>
	<script type="text/javascript">
		google.load("jquery", "1.6.1");
		google.load("swfobject", "2.2");
	</script>
	<script type="text/javascript" src="http://gae.cavelis.net/js/jquery.json-2.2.min.js"></script>
	<script type="text/javascript" src="http://ws.cavelis.net:3000/socket.io/socket.io.js"></script>
	</head>
	<body>
	<script type="text/javascript">
		var socket = null;			//socket.io
		//var comet_connect = false;		//接続しているかどうか (コメントアウトしてます)

		//cometの開始
		function start_comet() {
			//comet_connect = false;
			socket = io.connect('http://ws.cavelis.net:3000');

			//接続が開始したとき
			socket.on('connect' , function (msg) {
				//$("#ws_id").html(socket.socket.transport.sessid);	//接続のID
				//$("#ws_type").html(socket.socket.transport.name);	//接続のタイプ(websocket/flashsocketなど)
				//ソケット接続後に部屋にjoinします
				var msg = {};
				msg.mode = 'join';
				msg.room = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';
				socket.send($.toJSON(msg));
				//comet_connect = true;
			});
			
			//接続に失敗したプロトコル
			socket.on('connecting' , function (transport_type) {
			});
			//接続に失敗したとき
			socket.on('connect_failed' , function () {
				//comet_connect = false;
			});
			//再接続中
			socket.on('reconnecting' , function (reconnectionDelay , reconnectionAttemps) {
				//comet_connect = false;
			});
			//再接続されたとき
			socket.on('reconnect' , function (ransport_type, reconnectionAttemps) {
			});
			//再接続に失敗したとき
			socket.on('reconnection_failed' , function() {
			});
			
			//メッセージを受信したとき
			socket.on('message' , function (msg) {
				var json = $.parseJSON(msg);
				if ('mode' in json) {
					switch (json.mode) {
					case 'join':
					case 'leave':
						/*
						//joinやleaveでは接続や切断した情報が通知されます
						json.id		//接続や切断した人のid
						json.count	//接続している人の数(ip単位で重複あり)
						json.ipcount	//★接続している人の数(ip単位での重複を省く)
						*/
						break;
					case 'post':
						/*
						//ここでコメント情報が取得できます
						json.ret : true;		//★コメント情報があるかどうか
						json.id : 123456789:		//★投稿者のid
						json.message : "本文", 		//コメント本文
						json.html : "<div>本文</div>",	//HTML整形されたコメント
						json.listener : 8,		//現在の視聴者数(★この情報は2分毎更新なのでリアルタイムの情報ではない)
						json.max_listener : 10,		//最大視聴者数	(★この情報は2分毎更新なのでリアルタイムの情報ではない)
						json.viewer : 25,		//ページビュー
						json.comment_num : 125,		//このコメント番号
						json.name : "hoge",		//★コメント投稿者の名前
						json.time : 19190721		//★コメント投稿の日付 (単位はUnixTime 日本時間に直すにはタイムゾーン補正も必要),
						json.is_ban : false,		//★コメントがBANされているかどうか(notice: この時点ではis_banは必ずfalseです)
						json.auth:true,			//★コメントが認証コメント(緑コテ状態)かどうか
						*/
						if ('message' in json) {
							alert(json.message);
						}
						break;
					}
				}
			});
		}
		$(function() {
			start_comet();
		});
	</script>
	</body>
	</html>

## ★コメントの書き込み
当初はコメントの書き込みもWebSocketを使って通信しようとおもったのですが、HTTPリクエストの方が簡単そうだったので、HTTP(POST)を使った方法で書き込みます。(暇を見て、WebSocketを使った書き込み方法についても書いておきます)

POSTリクエストURL

	http://gae.cavelis.net/viewedit/postcomment

※GETリクエストは使えません。POSTリクエスト限定。

### postデータ例

* stream_name		//必須。投稿する配信のストリーム名(前述のroom_nameと同じ)
* name			//任意。投稿者の名前
* message			//必須。投稿内容

### レスポンス
受信データの書式はjson形式です。  
書式はWebSocketのpostデータととまったく一緒なのでそちらを参考にしてください。

	{
		ret : true,
		mode : "post",
		name : "hoge",
		time : 19190721,
		is_ban : false,
		auth : false,
		message : "ほげほげ",
		html : "<div>ほげほげ</div>",
		listener : 1,
		max_listener : 2,
		viewer : 10,
		comment_num : 15
	}

#### 注意点

セキュリティの観点から、認証されたコメントの投稿(緑コテ投稿)にはログインの認証情報が必須です。  
ブラウザから認証済みのcookie情報を取得した内容で、  さらにそのブラウザのSessionがKeepAliveな状態でコメントデータをそのブラウザから送信しないと、緑コテにならないです。  
この仕様だと、ブラウザごとにCookieのとり方などが変わってきたりなど、とても面倒だとおもうので、またhogeさんと相談して、簡単そうな実装方法を吟味する必要がありそうです。

⇒◆認証コテでコメントを書くには、apikeyを使うように変更しました。  
keyの取得の仕方については、  
http://gae.cavelis.net/api/key.txt をご覧ください。

参考:コテの種類

* 緑コテ
	* ログインしており、なおかつ名前欄に自分のログイン名を入れている状態。
	* 名前欄が緑色になり、他の人にはなりすましが出来ない状態です。  
	* 2chで言うところのトリップ付きの固定ハンドルです。  
* 名無し
	* 名前の欄に何も入れていない状態。
	* ログイン/非ログインに関わらず、名前欄の入力が無いと名無しになります。
	* 2chで言うところの名無しさんです。
* 黒コテ
	* 黒コテになるには2パターン考えられます。
	* パターン1:非ログイン状態で、名前欄に適当な名前を入れている状態。
	* パターン2:ログイン状態で、ログイン名と違う名前を入れている状態。
	* 2chで言うところのトリップ無し固定ハンドルです。

## 最後に
以上です。
コメント読み上げだけならHTTP通信とWebSocket通信でいけるとおもいます。
コメント欄の仕様は大幅な変更は無いと思いますが、今後小幅な仕様変更は出てくるかと思います。(特にWebSocket周り)
仕様変更があった場合、ここのtxtを更新しておきます。(開発者と直接連絡とれるようなら、連絡取るかも？)
レスポンスのjson情報が足りないのであれば、
こんな情報が欲しい!!と要望言ってくれれば、反映させます。

文責：かべりす

## ログ出力
開発者向けにnode.jsのログを公開ディレクトリに出力するようにしました  
ブラウザで下記のURLにアクセスすればログを確認できます。  
http://ws.cavelis.net:3000/console.txt  
http://ws.cavelis.net:3000/log4.txt  
websocketのエラー内容の確認などに使ってみてください。

## 更新履歴
2011/07/31

★マークの部分が今回の更新箇所です
hogeさんからのリクエストでいくつか書き出す情報を増やしました。

2011/07/17

初版

