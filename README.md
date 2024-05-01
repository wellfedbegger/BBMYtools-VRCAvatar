# BBMYtools - VRCAvatar

VRChat アバターをセットアップする際に役立つ Unity ツール集。

# 収録ツール一覧

## AvatarParameterViewer

指定した名前のパラメーターが、Avatar のどこで使用されているかを検索して一覧する。

### 検索範囲

- 各 Playable Layer に使用している各 Animator の、

  - Parameter
  - 各ステートの遷移条件
- `VRC Expression Parameters`
- `VRC Expression Menu`
- `VRC PhysBone` の `Parameter`
- `VRC Contact Receiver` の `Parameter`
- motion timeなど…

### 制約・未対応機能

一部は今後のアップデートで対応する場合がある。

- Modular Avatar で設定したパラメーター
- 部分一致検索
- Playable Layerを指定していない際に使用される、デフォルトAnimatorからの検索
