# UnitySceneNavigator

UniRxを統合してUniTask機能で作った、コードベースでシンプルなシーン遷移の管理システム。
まだ作業途中なので破壊的変更を入れまくります。

## 使い方

### 次のシーンに進む
```cs
// 次のシーンの引数を用意
var args = new NextSceneArgs();
    
// OnClickメソッドを使うことで連打時でも、内部の処理が終わるまでは二重に走らない
this.Button.OnClick(this.SceneShared, () => Navigator.NavigateAsync(args));
```

### ポップアップに進む
```cs
// ボタンを押した時にポップアップを開く
this.Button.OnClick(this.SceneShared, async () =>
{
    var args = new PopupSceneArgs();
    
    // ポップアップからの結果をawaitで待つ
    var result = await Navigator.NavigateAsPopupAsync<int>(args);
    
    // ポップアップから取得した結果で処理を行う
    await this.DoAfterPopup(result);
});
```

### 前のシーンに戻る
```cs
// ポップアップの呼びもとに結果を返したい場合は戻り値を送ることができる
this.Button.OnClick(this.SceneShared, () => Navigator.NavigateBackAsync(100));
```

### 動作確認
Unity 2018.3.0b12
