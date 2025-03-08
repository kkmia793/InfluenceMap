# InfluenceMap

## 概要
InfluenceMap（影響マップ）は、ゲーム内の脅威度やアイテムの狙いやすさをマップ上に反映します。
これにより敵（Ghosts）の位置やアイテム（Apples）の配置を基に、プレイヤーが安全で有利なポジションを把握できるようになります。


参考サイト: [Cygames Tech Blog](https://tech.cygames.co.jp/archives/2272/)

---

## 特徴
- **セル単位の可視化による影響度表示**
- **脅威度とアイテムの狙いやすさの加重合成**
- **ダイクストラ法による効率的な影響マップの更新**

---

## 工夫点

### 1. 脅威度とアイテムの狙いやすさの加重合成

- 影響マップのスコア計算において、脅威度（Threat）とアイテムの狙いやすさ（Attractiveness）を加重平均で合成しています。
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
- 例えば、敵をより警戒するAIを制作したい場合 `threatWeight` を高く設定し、より安全なルートを選択する行動を促すことができます。
- また、脅威度とアイテムの狙いやすさ以外の項目を追加することで様々な特性を持ったAIを量産することが可能です。

### 2. ダイクストラ法を活用した影響マップ更新

- 脅威マップ (`UpdateThreatMap`) とアイテムマップ (`UpdateItemMap`) の両方で、ダイクストラ法を使用した効率的な影響度計算を行っています。


```csharp
    private void UpdateThreatMap()
    {
        float[,] distanceMap = new float[gridWidth, gridHeight];
        bool[,] visited = new bool[gridWidth, gridHeight];
        PriorityQueue<Vector2Int> priorityQueue = new PriorityQueue<Vector2Int>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                distanceMap[x, y] = float.MaxValue;
                threatMap[x, y] = 0;
            }
        }

        // 敵の位置をキューに追加
        foreach (var ghost in ghosts)
        {
            if (ghost == null) continue;

            Vector2Int gridPosition = WorldToCell(ghost.position);
            if (IsValidGridPosition(gridPosition))
            {
                distanceMap[gridPosition.x, gridPosition.y] = 0;
                priorityQueue.Enqueue(gridPosition, 0);
            }
        }
        
        
        // ダイクストラ法
        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            int x = current.x;
            int y = current.y;

            if (visited[x, y]) continue;
            visited[x, y] = true;

            float currentDistance = distanceMap[x, y];
            float threat = Mathf.Max(0, maxThreatDistance - currentDistance);
            threatMap[x, y] = threat;

            foreach (var direction in directions)
            {
                Vector2Int neighbor = new Vector2Int(x + direction.x, y + direction.y);

                if (IsValidGridPosition(neighbor) && !visited[neighbor.x, neighbor.y] && !IsObstacleCell(neighbor))
                {
                    float newDistance = currentDistance + cellSize;
                    if (newDistance < distanceMap[neighbor.x, neighbor.y])
                    {
                        distanceMap[neighbor.x, neighbor.y] = newDistance;
                        priorityQueue.Enqueue(neighbor, newDistance);
                    }
                }
            }
        }
    }
```

- ダイクストラ法により、**影響源に近いエリアから順番に計算が進む**ため、無駄なセルの再計算を防ぎ、パフォーマンスの向上を実現しました。

### 3. スコアの正規化

- `CombineMaps` メソッド内で、スコアの最大値・最小値を用いて正規化を行っています。

```csharp
for (int x = 0; x < gridWidth; x++)
{
    for (int y = 0; y < gridHeight; y++)
    {
        if (!IsObstacleCell(new Vector2Int(x, y)))
        {
            combinedMap[x, y] = (combinedMap[x, y] - minScore) / Mathf.Max(maxScore - minScore, Mathf.Epsilon);
        }
    }
}
```

- 正規化することで、スコアが0〜1の範囲に収まるため、他のAIやシステムと統合しやすくなっています。
- 視覚化する際の色付けや、エージェントが行動を選択する際のしきい値設定も容易になります。

---

## 今後の改善点
- 影響マップのさらなるパフォーマンス最適化
- 3D空間での影響マップ実装

