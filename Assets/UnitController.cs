using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 駒のタイプ
public enum UnitType
{
    None = -1,
    Hu = 1,
    Kaku,
    Hisha,
    Kyousha,
    Keima,
    Gin,
    Kin,
    Gyoku,
    // 成
    Tokin,
    Uma,
    Ryu,
    NariKyo,
    NariKei,
    NariGin,
}

// 駒の場所
public enum FieldStatus
{
    OnBard,
    Captured,
}

public class UnitController : MonoBehaviour
{
    // ユニットのプレイヤー番号
    public int Player;
    // ユニットの種類
    public UnitType UnitType, OldUnitType;
    // ユニットの場所
    public FieldStatus FieldStatus;

    // 成テーブル
    Dictionary<UnitType, UnitType> evolutionTable = new Dictionary<UnitType, UnitType>()
    {
        {UnitType.Hu, UnitType.Tokin },
        {UnitType.Kaku, UnitType.Uma },
        {UnitType.Hisha, UnitType.Ryu },
        {UnitType.Kyousha, UnitType.NariKyo },
        {UnitType.Keima, UnitType.NariKei },
        {UnitType.Gin, UnitType.NariGin },
        {UnitType.Kin, UnitType.None },
        {UnitType.Gyoku, UnitType.None },
    };

    // ユニット選択/非選択のy座標
    public const float SelectUnitY = 1.5f;
    public const float UnSelectUnitY = 0.7f;

    // 置いている場所
    public Vector2Int Pos;

    // 選択される前のy座標
    float oldPosY;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // 初期設定
    public void Init(int player, int unittype, GameObject tile, Vector2Int pos)
    {
        Player = player;
        UnitType = (UnitType)unittype;
        // 取られた時元に戻るよう
        OldUnitType = (UnitType)unittype;
        // 場所の初期値
        FieldStatus = FieldStatus.OnBard;
        // 角度と場所
        transform.eulerAngles = getDefaultAngles(player);
        Move(tile, pos);
    }

    // 指定されたプレイヤー番号の向きを返す
    Vector3 getDefaultAngles(int player)
    {
        return new Vector3(90, player * 180, 0);
    }

    // 移動処理
    public void Move(GameObject tile, Vector2Int tileindex)
    {
        // 新しい場所へ移動する
        Vector3 pos = tile.transform.position;
        pos.y = UnSelectUnitY;
        transform.position = pos;
        // インデックス更新
        Pos = tileindex;
    }

    // 選択時の処理
    public void Select(bool select = true)
    {
        Vector3 pos = transform.position;
        bool iskinematic = select;

        if (select)
        {
            oldPosY = pos.y;
            pos.y = SelectUnitY;
        }
        else
        {
            pos.y = UnSelectUnitY;

            // 持ち駒の位置は特別
            if (FieldStatus.Captured == FieldStatus)
            {
                pos.y = oldPosY;
                iskinematic = true;
            }
        }

        GetComponent<Rigidbody>().isKinematic = iskinematic;
        transform.position = pos;
    }

    // 移動可能範囲取得
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkotherunit = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        // 持ち駒状態
        if (FieldStatus.Captured == FieldStatus)
        {
            foreach (var checkpos in getEmptyTiles(units))
            {
                // 移動可能
                bool ismovable = true;

                // 移動した状態を作る
                Pos = checkpos;
                FieldStatus = FieldStatus.OnBard;

                // 自分以外いないフィールドを作って、置いたあと移動できないなら移動不可
                UnitController[,] exunits = new UnitController[units.GetLength(0), units.GetLength(1)];
                exunits[checkpos.x, checkpos.y] = this;

                if (1 > getMovableTiles(exunits, UnitType).Count)
                {
                    ismovable = false;
                }

                // 歩
                if (UnitType.Hu == UnitType)
                {
                    // 二歩
                    for (int i = 0; i < units.GetLength(1); i++)
                    {
                        if (units[checkpos.x, i]
                            && UnitType.Hu == units[checkpos.x, i].UnitType
                            && Player == units[checkpos.x, i].Player)
                        {
                            ismovable = false;
                            break;
                        }
                    }

                    // 打ち歩詰め
                    int nextplayer = GameSceneDirector.GetNextPlayer(Player);

                    // 今回打ったことにして、王手になる場合
                    UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);
                    copyunits[checkpos.x, checkpos.y] = this;
                    int outecount = GameSceneDirector.GetOuteUnits(copyunits, nextplayer, false).Count;

                    if (0 < outecount && ismovable)
                    {
                        // 打ち歩詰め状態にしておく
                        ismovable = false;
                        // 相手のいずれかの駒が歩を取った状態を再現
                        foreach (var unit in units)
                        {
                            if (!unit || nextplayer != unit.Player) continue;
                            // 移動範囲にcheckposがない
                            if (!unit.GetMovableTiles(copyunits).Contains(checkpos)) continue;
                            // 相手の駒を移動させた状態を作る
                            copyunits[checkpos.x, checkpos.y] = unit;
                            // 再度王手されているかチェック
                            outecount = GameSceneDirector.GetOuteUnits(copyunits, nextplayer, false).Count;
                            // 1つでも王手を回避できる駒があれば打ち歩詰めじゃない
                            if (1 > outecount)
                            {
                                ismovable = true;
                            }
                        }
                    }

                }

                // 移動不可　次の場所を調べる
                if (!ismovable) continue;

                ret.Add(checkpos);
            }

            // 移動状態をもとに戻す
            Pos = new Vector2Int(-1, -1);
            FieldStatus = FieldStatus.Captured;
        }
        // 玉
        else if (UnitType.Gyoku == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);

            // 相手の移動範囲を考慮しない
            if (!checkotherunit) return ret;

            // 削除対象のタイル（敵の移動範囲）
            List<Vector2Int> removetiles = new List<Vector2Int>();

            foreach (var item in ret)
            {
                // 移動した状態を作って王手されているなら削除対象
                UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);
                // 今いる場所から移動した状態にする
                copyunits[Pos.x, Pos.y] = null;
                copyunits[item.x, item.y] = this;

                // 王手しているユニット数
                int outecount = GameSceneDirector.GetOuteUnits(copyunits, Player, false).Count;
                if (0 < outecount) removetiles.Add(item);
            }

            // ↑で取得したタイルを除外する
            foreach (var item in removetiles)
            {
                ret.Remove(item);
            }
        }
        // 金と同じ動き
        else if (UnitType.Tokin == UnitType
            || UnitType.NariKyo == UnitType
            || UnitType.NariKei == UnitType
            || UnitType.NariGin == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Kin);
        }
        // 馬(玉+角)
        else if (UnitType.Uma == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);
            foreach (var item in getMovableTiles(units, UnitType.Kaku))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }
        }
        // 龍(玉+飛車)
        else if (UnitType.Ryu == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);
            foreach (var item in getMovableTiles(units, UnitType.Hisha))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }
        }
        else
        {
            ret = getMovableTiles(units, UnitType);
        }


        return ret;
    }

    // もととなる移動可能範囲取得
    List<Vector2Int> getMovableTiles(UnitController[,] units, UnitType unittype)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        // 歩
        if (UnitType.Hu == unittype)
        {
            // 向き
            int dir = (0 == Player) ? 1 : -1;

            // 前方1マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(0, 1 * dir)
            };

            // 実際のフィールドを調べる
            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // 桂馬
        else if (UnitType.Keima == unittype)
        {
            // 向き
            int dir = (0 == Player) ? 1 : -1;

            // 前方斜め2マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 1, 2 * dir),
                new Vector2Int(-1, 2 * dir),
            };

            // 実際のフィールドを調べる
            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // 銀
        else if (UnitType.Gin == unittype)
        {
            // 向き
            int dir = (0 == Player) ? 1 : -1;

            // 前方斜め2マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 0, 1 * dir),
                new Vector2Int(-1, 1 * dir),
                new Vector2Int( 1, 1 * dir),
                new Vector2Int(-1,-1 * dir),
                new Vector2Int( 1,-1 * dir),
            };

            // 実際のフィールドを調べる
            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // 金
        else if (UnitType.Kin == unittype)
        {
            // 向き
            int dir = (0 == Player) ? 1 : -1;

            // 前方斜め2マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(-1, 1 * dir),
                new Vector2Int( 0, 1 * dir),
                new Vector2Int( 1, 1 * dir),
                new Vector2Int( 1, 0 * dir),
                new Vector2Int( 0,-1 * dir),
                new Vector2Int(-1, 0 * dir),
            };

            // 実際のフィールドを調べる
            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // 玉
        else if (UnitType.Gyoku == unittype)
        {
            // 周り1マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(-1, 1),
                new Vector2Int( 0, 1),
                new Vector2Int( 1, 1),
                new Vector2Int( 1, 0),
                new Vector2Int( 1,-1),
                new Vector2Int( 0,-1),
                new Vector2Int(-1,-1),
                new Vector2Int(-1, 0),
            };

            // 実際のフィールドを調べる
            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                if (!isCheckable(units, checkpos) || isFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // 角
        else if (UnitType.Kaku == unittype)
        {
            // 壁や他の駒とぶつかるまでどこまでも進める
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int( 1,-1),
                new Vector2Int(-1,-1),
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos))
                {
                    // 他の駒があった場合
                    if (units[checkpos.x, checkpos.y])
                    {
                        // 相手のユニットの場所へは移動可能
                        if (Player != units[checkpos.x, checkpos.y].Player)
                        {
                            ret.Add(checkpos);
                        }
                        break;
                    }

                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        // 飛車
        else if (UnitType.Hisha == unittype)
        {
            // 壁や他の駒とぶつかるまでどこまでも進める
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 0, 1),
                new Vector2Int( 0,-1),
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos))
                {
                    // 他の駒があった場合
                    if (units[checkpos.x, checkpos.y])
                    {
                        // 相手のユニットの場所へは移動可能
                        if (Player != units[checkpos.x, checkpos.y].Player)
                        {
                            ret.Add(checkpos);
                        }
                        break;
                    }

                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        // 香車
        else if (UnitType.Kyousha == unittype)
        {
            // 向き
            int dir = (0 == Player) ? 1 : -1;

            // 前方1マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(0, 1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                while (isCheckable(units, checkpos))
                {
                    // 他の駒があった場合
                    if (units[checkpos.x, checkpos.y])
                    {
                        // 相手のユニットの場所へは移動可能
                        if (Player != units[checkpos.x, checkpos.y].Player)
                        {
                            ret.Add(checkpos);
                        }
                        break;
                    }

                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }

        return ret;
    }

    // 配列オーバーかどうか
    bool isCheckable(UnitController[,] ary, Vector2Int idx)
    {
        // 配列オーバーの状態
        if (idx.x < 0 || ary.GetLength(0) <= idx.x
            || idx.y < 0 || ary.GetLength(1) <= idx.y)
        {
            return false;
        }
        return true;
    }

    // 仲間のユニットかどうか
    bool isFriendlyUnit(UnitController unit)
    {
        if (unit && Player == unit.Player) return true;
        return false;
    }

    // キャプチャされた時
    public void Capture(int player)
    {
        Player = player;
        FieldStatus = FieldStatus.Captured;
        Evolution(false);
        GetComponent<Rigidbody>().isKinematic = true;
    }

    // 成
    public void Evolution(bool evolution = true)
    {
        Vector3 angle = transform.eulerAngles;

        // 成
        if (evolution && UnitType.None != evolutionTable[UnitType])
        {
            UnitType = evolutionTable[UnitType];
            angle.x = 270;
            angle.y = (0 == Player) ? 180 : 0;
            angle.z = 0;
            transform.eulerAngles = angle;
        }
        else
        {
            UnitType = OldUnitType;
            transform.eulerAngles = getDefaultAngles(Player);
        }
    }

    // 空いているタイルを返す
    List<Vector2Int> getEmptyTiles(UnitController[,] units)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        for (int i = 0; i < units.GetLength(0); i++)
        {
            for (int j = 0; j < units.GetLength(1); j++)
            {
                if (units[i, j]) continue;
                ret.Add(new Vector2Int(i, j));
            }
        }
        return ret;
    }

    // 進化できるかどうか
    public bool isEvolution()
    {
        if (!evolutionTable.ContainsKey(UnitType) || UnitType.None == evolutionTable[UnitType])
        {
            return false;
        }
        return true;
    }
}
