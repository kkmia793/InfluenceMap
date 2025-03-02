# InfluenceMap

## 概要
InfluenceMap（影響マップ）は、Unityを使用してゲーム内の脅威度やアイテムの狙いやすさをマップ上に可視化するシステムです。
敵（Ghosts）の位置やアイテム（Apples）の配置を基に、プレイヤーが安全で有利なポジションを視覚的に把握できるようにしています。

参考サイト: [Cygames Tech Blog](https://tech.cygames.co.jp/archives/2272/)

---

## 特徴
- **セル単位の可視化による影響度表示**
- **脅威度とアイテムの狙いやすさの加重合成**

---

## 工夫点

### 1. 脅威度とアイテムの狙いやすさの加重合成

- インフルエンスマップのスコア計算において、脅威度（Threat）とアイテムの狙いやすさ（Attractiveness）を加重平均で合成しています。
- 脅威度は負の影響（避けたいエリア）として、アイテムの狙いやすさは正の影響（向かいたいエリア）として扱っています。

```csharp
private void CombineMaps()
{
    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            combinedMap[x, y] = -threatWeight * threatMap[x, y] + itemWeight * itemMap[x, y];
        }
    }
}
```

- `threatWeight` と `itemWeight` は調整可能なパラメータで、シチュエーションに応じて行動パターンを変化させることができます。
- 例えば、敵が多い場合は `threatWeight` を高く設定し、より安全なルートを選択する行動を促すことができます。

### 2. 距離による影響度の減衰処理

- 脅威やアイテムからの影響度を、距離に応じて減衰させるアルゴリズムを実装しています。

```csharp
private float CalculateThreat(Vector2 cellWorldPosition)
{
    float totalThreat = 0;
    foreach (var ghost in ghosts)
    {
        float distance = Vector2.Distance(cellWorldPosition, ghost.position);
        if (distance <= maxThreatDistance)
        {
            totalThreat += Mathf.Max(0, maxThreatDistance - distance);
        }
    }
    return totalThreat;
}
```

- 距離が `maxThreatDistance` を超えると影響度がゼロになるため、計算量を抑えつつも、近い要素を優先する行動が可能です。

### 3. スコアの正規化

- `CombineMaps` メソッド内で、スコアの最大値・最小値を用いて正規化を行っています。

```csharp
for (int x = 0; x < gridWidth; x++)
{
    for (int y = 0; y < gridHeight; y++)
    {
        if (!IsObstacleCell(new Vector2Int(x, y)))
        {
            combinedMap[x, y] = (combinedMap[x, y] - minScore) / (maxScore - minScore);
        }
    }
}
```

- 正規化により、スコアが0〜1の範囲に収まるため、他のAIやシステムと統合しやすくなっています。
- また、正規化することで視覚化する際の色付けや、エージェントが行動を選択する際のしきい値設定も容易になります。

---

## 今後の改善点
- 影響マップのパフォーマンス最適化
- マルチスレッド化による実行速度の向上

