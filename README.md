# Unity GetComponent Automation Library

属性ベースでコンポーネントを自動取得するUnityライブラリ。
※Awakeで解決されるため、取得後の処理はStart()に記述すること。

## 機能

### 対応メンバ（複合可）
- public, protected, privateメンバ
- フィールド変数
- プロパティ
- 配列
- インターフェイス
- 親子階層
- 兄弟階層

## 使い方

### シーンでの使用

1. シーンに`GetComponentManager`をアタッチしたGameObjectを配置
2. コンポーネントに属性を記述
3. 実行時に自動でコンポーネントが取得される

### プレハブでの使用

プレハブには`AutoGetComponentForPrefab`コンポーネントが必要。  
手動での追加よりもエディタウィンドウからの追加を推奨。

#### 手動で追加
プレハブに`AutoGetComponentForPrefab`を直接アタッチ

#### 一括管理（推奨）
1. `Tools > Mantensei > GetComponent Prefab Manager`を開く
2. プロジェクト内のプレハブが自動でスキャンされる
3. 「正規化を実行」ボタンで必要なプレハブに自動でアタッチ

### 制約事項

- `[Parent]`は1コンポーネントにつき1つまで
- 親階層のTransformを取得するのは非推奨（設計の柔軟性を損なうため。親にはマーカーとなるコンポーネントを付けるとよい）
- `HierarchyRelation.All`は非推奨（パフォーマンス上の理由）

## 属性リファレンス

### 基本属性

| 属性 | 説明 |
|------|------|
| `[GetComponent]` | 自身からコンポーネントを取得 |
| `[GetComponent(HierarchyRelation.Parent)]` | 親から取得 |
| `[GetComponent(HierarchyRelation.Children)]` | 子から取得 |
| `[GetComponent(HierarchyRelation.Self \| HierarchyRelation.Parent)]` | 複合条件（自身または親） |
| `[GetComponents]` | 配列で取得 |
| `[AddComponent]` | コンポーネントがなければ追加 |

### Parent-Sibling属性

| 属性 | 説明 |
|------|------|
| `[Parent]` | 親を取得 |
| `[Sibling]` | 親配下のコンポーネントを1つ取得（Self + Children）。※当ライブラリは基本的に自身は含まないが、これに関しては含む |
| `[Siblings]` | 親配下のコンポーネントを配列で取得。※当ライブラリは基本的に自身は含まないが、これに関しては含む |

### 使用例

#### 基本的な取得
```csharp
public class PlayerController : MonoBehaviour
{
    // 自身から取得
    [GetComponent] private WeaponController weaponController;                  
    // 自身または親から取得
    [GetComponent(HierarchyRelation.Self | HierarchyRelation.Parent)] private CharacterController root;  
    // 子から配列で取得
    [GetComponents(HierarchyRelation.Children)] private Weapon[] weapons;                                   
    // 自身になければ追加
    [AddComponent] private PlayerAnimation playerAnim;                          
}
```

階層構造：
```
Root (CharacterController)
└─ PlayerController (WeaponController) ← ここにスクリプト
   └─ Weapons
      ├─ Sword (Weapon)
      └─ Bow (Weapon)
```

#### Parent-Siblingパターン
```csharp
public class PlayerController : MonoBehaviour
{
    [Parent] private CharacterController root;   // 親を取得
    [Sibling] private WeaponSlot weaponSlot;     // 兄弟を単一取得
    [Siblings] private Weapon[] weapons;         // 兄弟配下の武器を配列で取得
}
```

階層構造：
```
Root (CharacterController)
├─ PlayerController ← ここにスクリプト
└─ WeaponSlot
   ├─ Sword (Weapon)
   └─ Bow (Weapon)
```

## 補足

### Unity fake null対応について

本ライブラリでは、Destroyされたオブジェクトの安全なnullチェックのため、内部で`as Object != null`を使用しています。

本来は[UnityObjectSafetyExtensions](https://mantensei.hatenablog.com/entry/2024/12/14/224852)の拡張メソッドを使用したかったのですが、依存を最小限に抑えるため独自実装としています。

## ライセンス

MIT License