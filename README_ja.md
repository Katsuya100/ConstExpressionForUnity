# ConstExpressionForUnity
## 概要
本ライブラリ「ConstExpressionForUnity」はコンパイル時計算ライブラリです。  
コンパイル時に定数計算処理を見つけ、計算結果のみをコード中に埋め込むことができます。  
これを利用することで、ハッシュ計算やド・ブラウン列計算のような負荷の高い処理をゼロコストで実装できます。  

## 動作確認環境
|  環境  |  バージョン  |
| ---- | ---- |
| Unity | 2021.3.15f1, 2022.3.2f1 |
| .Net | 4.x, Standard 2.1 |

## ConstExpressionの実行例
### 実装例
```MyConstExpression.cs
using Katuusagi.ConstExpressionForUnity;

public static class MyConstExpression
{
    [ConstExpression]
    public static int Add(int l, int r)
    {
        return l + r;
    }
}
```

```Test.cs
using UnityEngine;

public static class Test
{
    public static void Run()
    {
        Debug.Log(MyConstExpression.Add(100, 200));
    }
}
```

### コンパイル後のコード(with ILSpy)
```Test.cs
using UnityEngine;

public static class Test
{
	public static void Run()
	{
		Debug.Log((object)300);
	}
}
```
このように、不要なランタイム計算を省略し、計算結果のみをコードに埋め込むことができます。  

## 性能
### エディタ上の計測コード
[テストコード](packages/Tests/Runtime/ConstExpressionPerformanceTest.cs)  
エラトステネスの篩による素数計算を複数回行っています。

#### 結果

|  実行処理  |  処理時間  |
| ---- | ---- |
| CalcPrime_ConstExpression | 0.056805 ms |
| CalcPrime_Raw | 78.87844 ms |

計算そのものが消失し計算結果のみを返すため、コストが殆どかからなくなっています。  

## インストール方法
### ILPostProcessorCommonのインストール
[ILPostProcessorCommon v1.1.0](https://github.com/Katsuya100/ILPostProcessorCommon/tree/v1.1.0) を参照しインストールする。

### ConstExpressionForUnityのインストール
1. [Window > Package Manager]を開く。
2. [+ > Add package from git url...]をクリックする。
3. `https://github.com/Katsuya100/ConstExpressionForUnity.git?path=packages`と入力し[Add]をクリックする。

#### うまくいかない場合
上記方法は、gitがインストールされていない環境ではうまく動作しない場合があります。
[Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases)から該当のバージョンの`com.katuusagi.constexpressionforunity.tgz`をダウンロードし
[Package Manager > + > Add package from tarball...]を使ってインストールしてください。

#### それでもうまくいかない場合
[Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases)から該当のバージョンの`Katuusagi.ConstExpressionForUnity.unitypackage`をダウンロードし
[Assets > Import Package > Custom Package]からプロジェクトにインポートしてください。

## 使い方
### 通常の使用法
#### ConstExpression用アセンブリの作成
1. Projectブラウザ上の任意のフォルダで[右クリック > Create > Folder]を選択します。
2. フォルダ名を設定します。（例:ConstExpressionEntry）
3. 2のフォルダを[右クリック > Create > Assembly Definition Reference]を選択します。
4. アセット名を設定します。（例:`ConstExpressionEntry.asmref`）
5. 4のアセットをクリックしInspector上で`Assembly Definition`に`ConstExpressionEntry`を設定します。

#### 計算関数を実装する
ConstExpression用アセンブリのフォルダ以下に下記のようにスクリプトを実装する。
```.cs
using Katuusagi.ConstExpressionForUnity;

public static class MyConstExpression
{
    [ConstExpression]
    public static int Add(int l, int r)
    {
        return l + r;
    }
}
```
`ConstExpression`属性をつけた関数はコンパイル時定数計算の対象になります。  

### コンパイル時定数計算の制約
コンパイル時定数計算を行うにはいくつかの制約があります。

#### ConstExpression属性
`ConstExpression`属性のついている関数のみコンパイル時定数計算の対象になります。

#### static関数
`ConstExpression`属性はstatic関数にのみつけることができます。

#### 引数
`ConstExpression`属性は以下の引数型を持つ関数にのみつけることができます。  
 <table>
    <tr>
      <td>string</td>
      <td>char</td>
      <td>sbyte</td>
      <td>byte</td>
    </tr>
    <tr>
      <td>short</td>
      <td>ushort</td>
      <td>int</td>
      <td>uint</td>
    </tr>
    <tr>
      <td>long</td>
      <td>ulong</td>
      <td>float</td>
      <td>double</td>
    </tr>
    <tr>
      <td>bool</td>
      <td>enum</td>
      <td></td>
      <td></td>
    </tr>
 </table>

また、コンパイル時定数計算を行うには、引数に代入する値が定数でなければなりません。  
定数を代入しない場合、警告が発生します。
```.cs
// OK
MyConstExpression.Add(100, 200);

// OK
const int l = 100;
const int r = 200;
MyConstExpression.Add(l, r);

// NG
int l = 100;
int r = 200;
MyConstExpression.Add(l, r);

// NG
readonly int l = 100;
readonly int r = 200;
MyConstExpression.Add(l, r);
```
##### CalculationFailedWarningオプション
`ConstExpression`属性に`CalculationFailedWarning=false`を設定すると
引数に定数以外の値を代入しても警告が発生しなくなります。
```
[ConstExpression(CalculationFailedWarning = false)]
public static int Add(int l, int r)
{
    return l + r;
}

// 警告が発生しない
int l = 100;
int r = 200;
MyConstExpression.Add(l, r);
```

#### 戻り値
`ConstExpression`属性は以下の戻り値型を持つ関数にのみつけることができます。  

 <table>
    <tr>
      <td>string</td>
      <td>char</td>
      <td>sbyte</td>
      <td>byte</td>
    </tr>
    <tr>
      <td>short</td>
      <td>ushort</td>
      <td>int</td>
      <td>uint</td>
    </tr>
    <tr>
      <td>long</td>
      <td>ulong</td>
      <td>float</td>
      <td>double</td>
    </tr>
    <tr>
      <td>bool</td>
      <td>enum</td>
      <td>struct<br/>- Marshal.SizeOf可能であること<br/>- classをメンバに持たないこと</td>
      <td></td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltstring&gt</td>
      <td>ReadOnlyArray&ltchar&gt</td>
      <td>ReadOnlyArray&ltsbyte&gt</td>
      <td>ReadOnlyArray&ltbyte&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltshort&gt</td>
      <td>ReadOnlyArray&ltushort&gt</td>
      <td>ReadOnlyArray&ltint&gt</td>
      <td>ReadOnlyArray&ltuint&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltlong&gt</td>
      <td>ReadOnlyArray&ltulong&gt</td>
      <td>ReadOnlyArray&ltfloat&gt</td>
      <td>ReadOnlyArray&ltdouble&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltbool&gt</td>
      <td>ReadOnlyArray&ltenum&gt</td>
      <td></td>
      <td></td>
    </tr>
 </table>

structとReadOnlyArrayはリテラル化できないため、static readonly変数として保持されています。

### ReadOnlyArrayについて
ReadOnlyArrayはConstExpressionの戻り値定数として用意した不変の配列です。  
IReadOnlyListを継承している他、ReadOnlySpanへの暗黙的キャストが可能です。  
配列からの暗黙的キャストも可能です。  
```.cs
// OK
ReadOnlyArray<byte> binary = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

// OK
ReadOnlySpan<byte> spanBinary = binary;

// OK
IReadOnlyList<byte> listBinary = binary;

// NG
byte[] bytes = binary;
```
