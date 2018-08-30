﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Map
{
    public enum LinkedShape { NONE, HORIZONTAL, VERTICAL };

    public class MapManager : MonoBehaviourSingleton<MapManager>
    {
        [SerializeField]
        ObjectPool objectPool;
        [Space(10)]
        [Header("variable")]
        public int width = 1;
        public int height = 1, max = 17, mini = 6;
        public float maxHallRate = 0.15f;
        public readonly int size = 3;
        [SerializeField]
        bool Debug;
        bool isBossRush;
        Map map;
        private void Start()
        {
            if(!Debug)
            {
                switch (GameStateManager.Instance.GetMode())
                {
                    case GameStateManager.GameMode.NORMAL:
                        isBossRush = false;
                        break;
                    case GameStateManager.GameMode.RUSH:
                        isBossRush = true;
                        break;
                    default:
                        isBossRush = false;
                        break;
                }
            }
            else
            {
                isBossRush = true;
                GameStateManager.Instance.SetMode(GameStateManager.GameMode.RUSH);
            }
        }
        public void GenerateMap(int _floor)
        {
            if (map != null)
            {
                map = null;
            }
            if (isBossRush)
            {
                map = new BossRushMap(width, height, max, mini, maxHallRate, objectPool);
                map.Generate();
                RoomManager.Instance.InitRoomList();
            }
            else
            {
                map = new Map(width, height, max, mini, maxHallRate, objectPool);
                map.AddNecessaryRoomSet(RoomSetManager.Instance.firstFloorSet);
                map.Generate();
                map.AddFallRock();
                RoomManager.Instance.InitRoomList();
            }
        }
        public Map GetMap()
        {
            return map;
        }
        public bool GetRushMode()
        {
            return isBossRush;
        }
    }

    public class Map
    {
        #region dataStruct
        protected Queue<Rect> rects, blocks;
        protected List<Rect> halls, rooms;
        protected List<RoomSet> necessaryRoomSet, settedRoomSet;
        protected Vector3 startPosition;
        #endregion
        #region components
        protected Tilemap verticalWallTileMap, horizonWallTileMap, floorTileMap, shadowTileMap, fogTileMap;
        protected Rect mainRect;
        protected ObjectPool objectPool;
        #endregion

        protected float MaxHallRate = 0.15f;
        protected int MaximumRoomArea = 4;
        protected int MinimumRoomArea = 6;
        protected int TotalHallArea = 0;
        protected int width;
        protected int height;
        protected const int size = 3;

        public Map(int _width, int _height, int _max, int _mini, float _maxHallRate, ObjectPool _objectPool)
        {
            mainRect = new Rect(0, 0, _width, _height, 3);
            width = _width;
            height = _height;
            MaximumRoomArea = _max;
            MinimumRoomArea = _mini;
            MaxHallRate = _maxHallRate;
            objectPool = _objectPool;
            rects = new Queue<Rect>();
            blocks = new Queue<Rect>();
            halls = new List<Rect>(_width * _height);
            rooms = new List<Rect>(_width * _height);
            floorTileMap = TileManager.Instance.floorTileMap;
            shadowTileMap = TileManager.Instance.shadowTileMap;
            verticalWallTileMap = TileManager.Instance.verticalWallTileMap;
            horizonWallTileMap = TileManager.Instance.horizonWallTileMap;
            fogTileMap = TileManager.Instance.fogTileMap;
        } // 생성자

        #region public
        public List<Rect> GetList(out Rect currentRoom)
        {
            currentRoom = halls[0];
            return rooms;
        }

        public void AddFallRock()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (!rooms[i].isRoom)
                {
                    GameObject obj = ResourceManager.Instance.objectPool.GetPooledObject();
                    obj.transform.position = rooms[i].GetAvailableArea();
                    obj.AddComponent<FallRockTrap>();
                    obj.GetComponent<FallRockTrap>().Init(ResourceManager.Instance.Rock);
                }
            }
        }

        public Vector3 GetStartPosition()
        {
            return startPosition;
        }

        public void RemoveFog(Rect rect)
        {
            for (int x = rect.x * size + 1; x < (rect.x + rect.width) * size; x++)
            {
                for (int y = rect.y * size; y < (rect.y + rect.height) * size - 1; y++)
                {
                    fogTileMap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        public void Generate()
        {
            ClearTile();
            CreateMap();
            LinkAllRects();
            AssignAllHalls();
            rooms.AddRange(halls);
            BakeMap();
            LinkRecursion(); // 보스 방을 제외한 방 연결
            LinkBossRoom(); // 보스 방 연결
            LinkHall(); // 홀 연결
            DrawTile();
            BakeAvailableArea();
        } // office creates

        public void AddNecessaryRoomSet(RoomSet[] _roomSet)
        {
            int maxSize = width * height;
            int sum = 0;
            necessaryRoomSet = new List<RoomSet>(_roomSet.Length);
            settedRoomSet = new List<RoomSet>(_roomSet.Length);
            for (int i = 0; i < _roomSet.Length; i++)
            {
                sum += _roomSet[i].width * _roomSet[i].height;
                if (sum > maxSize)
                    break;
                if (_roomSet[i].width * _roomSet[i].height > MaximumRoomArea || _roomSet[i].width * _roomSet[i].height < MinimumRoomArea)
                    continue;
                necessaryRoomSet.Add(_roomSet[i]);
            }
        } // 필수 방 세팅
        #endregion
        #region private
        void DrawEventTile(Rect rect, RandomTile tile)
        {
            Tilemap tilemap = TileManager.Instance.EventFloorTileMap;
            for (int x = rect.x * size * 2; x < ((rect.x + rect.width) * size) * 2; x++)
            {
                for (int y = rect.y * size * 2; y < ((rect.y + rect.height) * size) * 2; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        void DrawHallTile(Rect rect, RandomTile tile)
        {
            for (int x = rect.x * size * 2; x < ((rect.x + rect.width) * size) * 2; x++)
            {
                for (int y = rect.y * size * 2; y < ((rect.y + rect.height) * size) * 2; y++)
                {
                    floorTileMap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        void BakeMap()
        {
            AStar.TileGrid.Instance.Bake();
            AStar.Pathfinder.Instance.Bake();
        }

        void RefreshData()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].customObjects != null)
                    for (int j = 0; j < rooms[i].customObjects.Length; j++)
                        Object.DestroyImmediate(rooms[i].customObjects[j].GetComponent<CustomObject>());
                if (rooms[i].doorObjects != null)
                    for (int j = 0; j < rooms[i].doorObjects.Count; j++)
                        Object.DestroyImmediate(rooms[i].doorObjects[j].GetComponent<CustomObject>());
            }

            objectPool.Deactivation();

            rects.Clear();
            halls.Clear();
            blocks.Clear();
            rooms.Clear();
            TotalHallArea = 0;

        } // 데이터 초기화

        void CreateMap()
        {
            int count = 0;

            while (true)
            {
                SettedRoomset();
                count++;
                Random.InitState((int)System.DateTime.Now.Ticks);
                RefreshData();
                rects.Enqueue(mainRect);
                RectToBlock();
                BlockToRoom();
                AssignAllRoom();
                if (null == necessaryRoomSet)
                    break;
                if (necessaryRoomSet.Count == 0)
                    break;
                if (count > 30)
                {
                    break;
                }
            }

            TileManager.Instance.verticalWallRuleTile.DeleteNull();
            TileManager.Instance.horizonWallRuleTile.DeleteNull();

        } // 맵 만들기 

        void SettedRoomset()
        {
            if (null == settedRoomSet)
                return;
            for (int i = 0; i < settedRoomSet.Count; i++)
            {
                necessaryRoomSet.Add(settedRoomSet[i]);
            }
            settedRoomSet.Clear();
        }

        void ClearTile()
        {
            floorTileMap.ClearAllTiles();
            verticalWallTileMap.ClearAllTiles();
            horizonWallTileMap.ClearAllTiles();
            shadowTileMap.ClearAllTiles();
        }

        void DrawTile()
        {
            RandomTile floor = TileManager.Instance.floorTile;
            RuleTile shadow = TileManager.Instance.shadowTile;
            RuleTile verticalRuleTile = TileManager.Instance.verticalWallRuleTile;
            RuleTile horizonRuleTile = TileManager.Instance.horizonWallRuleTile;
            RuleTile fogTile = TileManager.Instance.fogTile;

            Rect rect;

            for (int i = 0; i <= width * size * 2; i++)
            {
                for (int j = -1; j < height * size * 2; j++)
                {
                    floorTileMap.SetTile(new Vector3Int(i, j, 0), floor);
                }
            } // 바닥 그리기
            for (int x = 0; x <= width * size; x++)
            {
                for (int y = -1; y < height * size; y++)
                {
                    if (x == 0 || y == -1 || x == width * size || y == height * size - 1)
                    {
                        if (y == height * size - 1)
                        {
                            shadowTileMap.SetTile(new Vector3Int(x, y - 1, 0), shadow);
                        }
                        verticalWallTileMap.SetTile(new Vector3Int(x, y, 0), verticalRuleTile);
                        horizonWallTileMap.SetTile(new Vector3Int(x, y, 0), horizonRuleTile);
                    }
                }
            } // 맵 테두리 그리기
            for (int index = 0; index < rooms.Count; index++)
            {
                rect = rooms[index];
                if (!rect.isRoom)
                {
                    DrawHallTile(rect, TileManager.Instance.hallTile);
                    continue;
                }
                if (rect.eRoomType == RoomType.STORE)
                {
                    DrawEventTile(rect, TileManager.Instance.cafeTile);
                }
                else if (rect.eRoomType == RoomType.REST)
                {
                    DrawEventTile(rect, TileManager.Instance.restTile);
                }
                int minX = rect.x * size;
                int maxX = (rect.x + rect.width) * size - 1;
                int minY = rect.y * size;
                int maxY = (rect.y + rect.height) * size - 1;
                for (int x = rect.x * size; x < (rect.x + rect.width) * size; x++)
                {
                    for (int y = rect.y * size; y < (rect.y + rect.height) * size; y++)
                    {
                        fogTileMap.SetTile(new Vector3Int(x, y, 0), fogTile);

                        if (x == minX || y == minY ||
                                    x == maxX || y == maxY)
                        {
                            if (x == minX
                                && y == minY)
                            {
                                fogTileMap.SetTile(new Vector3Int(x, y - 1, 0), fogTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x, y, 0), verticalRuleTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y, 0), horizonRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 1, 0), shadow);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 2, 0), shadow);
                            }
                            else if (x == maxX && y == minY)
                            {
                                fogTileMap.SetTile(new Vector3Int(x, y - 1, 0), fogTile);
                                fogTileMap.SetTile(new Vector3Int(x + 1, y, 0), fogTile);
                                fogTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), fogTile);

                                verticalWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), verticalRuleTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), verticalRuleTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), horizonRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), horizonRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 2, 0), shadow);
                                shadowTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), shadow);
                                shadowTileMap.SetTile(new Vector3Int(x + 1, y - 2, 0), shadow);
                            }
                            else if (x == maxX && y == maxY)
                            {
                                fogTileMap.SetTile(new Vector3Int(x + 1, y, 0), fogTile);
                                fogTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), fogTile);

                                verticalWallTileMap.SetTile(new Vector3Int(x, y, 0), verticalRuleTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), verticalRuleTile);
                                verticalWallTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y, 0), horizonRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), horizonRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 1, 0), shadow);
                                shadowTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), shadow);
                                shadowTileMap.SetTile(new Vector3Int(x + 1, y - 2, 0), shadow);

                            }
                            else if (y == minY)
                            {
                                fogTileMap.SetTile(new Vector3Int(x, y - 1, 0), fogTile);

                                verticalWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y - 1, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 2, 0), shadow);
                            }
                            else if (x == maxX)
                            {
                                fogTileMap.SetTile(new Vector3Int(x + 1, y, 0), fogTile);

                                verticalWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x + 1, y, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x + 1, y - 1, 0), shadow);
                            }
                            else
                            {
                                verticalWallTileMap.SetTile(new Vector3Int(x, y, 0), verticalRuleTile);
                                horizonWallTileMap.SetTile(new Vector3Int(x, y, 0), horizonRuleTile);
                                shadowTileMap.SetTile(new Vector3Int(x, y - 1, 0), shadow);
                            }
                        }
                    }
                }
            } // 방그리기

        } // 맵 그리기

        protected virtual void RectToBlock()
        {
            Rect rect;

            while (rects.Count > 0 && ((float)TotalHallArea / mainRect.area < MaxHallRate))
            {
                rect = rects.Dequeue();
                if (rect.area > MaximumRoomArea)
                    SplitHall(rect);
                else blocks.Enqueue(rect);
            }

            while (rects.Count > 0)
            {
                rect = rects.Dequeue();
                if (rect.area > 1)
                    blocks.Enqueue(rect);
            }

        } // Rects -> Blocks;

        protected virtual bool BlockToRoom()
        {
            Rect block;
            while (blocks.Count > 0)
            {
                block = blocks.Dequeue();
                if (block.width == 0 || block.height == 0)
                    continue;
                if ((block.area > MaximumRoomArea || (block.area > 2 && (float)block.width / block.height >= 2 || (float)block.height / block.width >= 2)))
                    SplitBlock(block);
                else
                {
                    if (block.area < MinimumRoomArea)
                    {
                        block.isRoom = false;
                        halls.Add(block);
                    }
                    else
                    {
                        block.IsRoom();
                        rooms.Add(block);
                    }
                }
            }

            return true;
        } // Blocks -> Rooms;

        protected virtual void SplitHall(Rect _currentRect)
        {
            Rect hall = null;

            Rect rect_a = null, rect_b = null;
            RandomBlockSplit(_currentRect, out rect_a, out hall, out rect_b);

            rects.Enqueue(rect_a);
            rects.Enqueue(rect_b);
            hall.isRoom = false;
            hall.isClear = true;
            halls.Add(hall);
            TotalHallArea += hall.area;
        } // split rects -> rects & halls

        void SplitBlock(Rect _currentBlock)
        {
            Rect block_a = null;
            Rect block_b = null;
            RandomRoomSplit(_currentBlock, out block_a, out block_b);

            blocks.Enqueue(block_a);
            blocks.Enqueue(block_b);
        } // split blocks -> blocks

        void RandomBlockSplit(Rect _currentRect, out Rect _rectA, out Rect _hall, out Rect _rectB)
        {
            bool flag = true;

            if (_currentRect.width > _currentRect.height)
                flag = true;
            else if (_currentRect.width < _currentRect.height)
                flag = false;
            else
            {
                if (UtilityClass.CoinFlip(50))
                    flag = true;
                else
                    flag = false;
            }

            if (flag)
            {
                int x1 = (int)((_currentRect.x + 0.5f) + _currentRect.width * (float)Random.Range(3, 8) / 10);
                _rectA = new Rect(_currentRect.x, _currentRect.y, x1 - _currentRect.x, _currentRect.height, size);
                _hall = new Rect(_rectA.x + _rectA.width, _currentRect.y, 1, _currentRect.height, size);
                _rectB = new Rect(_hall.x + _hall.width, _currentRect.y, _currentRect.width - _rectA.width - _hall.width, _currentRect.height, size);
            }
            else
            {
                int y1 = (int)((_currentRect.y + 0.5f) + _currentRect.height * (float)Random.Range(3, 8) / 10);
                _rectA = new Rect(_currentRect.x, _currentRect.y, _currentRect.width, y1 - _currentRect.y, size);
                _hall = new Rect(_currentRect.x, _rectA.y + _rectA.height, _currentRect.width, 1, size);
                _rectB = new Rect(_currentRect.x, _hall.y + _hall.height, _currentRect.width, _currentRect.height - _rectA.height - _hall.height, size);
            }

        } // split 덩어리 and 홀 and 덩어리

        bool RandomRoomSplit(Rect _currentBlock, out Rect _blockA, out Rect _blockB)
        {
            bool flag = true;

            if (_currentBlock.width > _currentBlock.height)
                flag = true;
            else if (_currentBlock.width < _currentBlock.height)
                flag = false;
            else
            {
                if (UtilityClass.CoinFlip(50))
                    flag = true;
                else
                    flag = false;
            }

            if (flag) // 가로
            {
                int width = (int)((_currentBlock.width + 0.5f) * (float)Random.Range(3, 8) / 10);
                _blockA = new Rect(_currentBlock.x, _currentBlock.y, width, _currentBlock.height, size);
                _blockB = new Rect(_currentBlock.x + width, _currentBlock.y, _currentBlock.width - width, _currentBlock.height, size);
            }
            else
            {
                int height = (int)((_currentBlock.height + 0.5f) * (float)Random.Range(3, 8) / 10);
                _blockA = new Rect(_currentBlock.x, _currentBlock.y, _currentBlock.width, height, size);
                _blockB = new Rect(_currentBlock.x, _currentBlock.y + height, _currentBlock.width, _currentBlock.height - height, size);
            }
            return true;
        } // split 방 and 방

        void MergeHalls()
        {
            LinkedShape linkedShape;
            bool isMerge;
            int i = 0;
            Rect tempRect;
            for (i = 0; i < halls.Count; i++)
            {
                for (int j = 0; j < halls[i].edgeRect.Count; ++j)
                {
                    tempRect = halls[i];
                    linkedShape = CheckLinkedShape(tempRect, tempRect.edgeRect[j]);

                    if (linkedShape == LinkedShape.NONE)
                        continue;


                    isMerge = tempRect.Merge(ref halls, tempRect.edgeRect[j], linkedShape);

                    if (isMerge)
                    {
                        i = 0;
                        j = -1;
                    }
                }
            }

            for (i = 0; i < halls.Count; i++)
            {
                halls[i].Drawing(Color.red, 0);
            }
        }

        void LinkAllRects()
        {
            for (int i = 0; i < halls.Count; i++)
            {
                for (int k = 0; k < halls.Count; k++)
                {
                    LinkRects(halls[i], halls[k]);
                }
            }
            MergeHalls();

            for (int i = 0; i < rooms.Count; i++)
            {
                for (int k = 0; k < halls.Count; k++)
                {
                    LinkRects(rooms[i], halls[k]);
                }
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    LinkRects(rooms[i], rooms[j]);
                }
            }
        } // 모든 Rects(Rooms and Halls) 연결

        LinkedShape CheckLinkedShape(Rect _rectA, Rect _rectB)
        {
            if ((Mathf.Abs(_rectA.midX - _rectB.midX) == (float)(_rectA.width + _rectB.width) / 2) &&
                           (Mathf.Abs(_rectA.midY - _rectB.midY) < (float)(_rectA.height + _rectB.height) / 2)) // 가로로 연결된 방
            {
                return LinkedShape.HORIZONTAL;
            }
            else if ((Mathf.Abs(_rectA.midX - _rectB.midX) < (float)(_rectA.width + _rectB.width) / 2) &&
                (Mathf.Abs(_rectA.midY - _rectB.midY) == (float)(_rectA.height + _rectB.height) / 2)) // 세로로 연결된 방
            {
                return LinkedShape.VERTICAL;
            }

            return LinkedShape.NONE;
        }

        void LinkRects(Rect _rectA, Rect _rectB) // 두개의 방을 직접 연결
        {
            LinkedShape result = CheckLinkedShape(_rectA, _rectB);
            if (result == LinkedShape.HORIZONTAL)
            {
                _rectA.EdgeRect(_rectB);
            }
            else if (result == LinkedShape.VERTICAL)
            {
                if (_rectA.midY > _rectB.midY)
                {
                    if (_rectB.eRoomType != RoomType.REST && _rectB.eRoomType != RoomType.STORE)
                        _rectA.EdgeRect(_rectB);
                    else
                        _rectA.LinkedEdgeRect(_rectB);
                }
                else
                {
                    if (_rectA.eRoomType != RoomType.REST && _rectA.eRoomType != RoomType.STORE)
                        _rectA.EdgeRect(_rectB);
                    else
                        _rectA.LinkedEdgeRect(_rectB);

                }
            }
        }

        void LinkRecursion()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].eRoomType != RoomType.BOSS)
                {
                    RecursionLink(rooms[i]);
                    break;
                }
            }
        } // 모두 연결

        void LinkBossRoom()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].eRoomType == RoomType.BOSS)
                {
                    for (int index = 0; index < rooms[i].edgeRect.Count; index++)
                    {
                        if (rooms[i].isRoom || rooms[i].edgeRect[i].isRoom)
                        {
                            DrawDoorTile(rooms[i], rooms[i].edgeRect[index]); //문 놓을 곳에 타일 지우기
                            rooms[i].LinkedEdgeRect(rooms[i].edgeRect[index]);
                            break;
                        }
                    }
                    break;
                }
            }
        } // 보스 방 연결

        void RecursionLink(Rect _rect)
        {
            if (_rect.eRoomType == RoomType.BOSS)
                return;
            _rect.visited = true;

            for (int i = 0; i < _rect.edgeRect.Count; i++)
            {
                if (!_rect.edgeRect[i].visited/*방문한 적 없어야함*/ && _rect.edgeRect[i].eRoomType != RoomType.BOSS)
                {
                    if ((_rect.isRoom || _rect.edgeRect[i].isRoom)/*둘 중 하나는 방이어야함*/)
                        DrawDoorTile(_rect, _rect.edgeRect[i]); //문 놓을 곳에 타일 지우기
                    _rect.LinkedEdgeRect(_rect.edgeRect[i]);
                    RecursionLink(_rect.edgeRect[i]);
                }
            }
        } // 신장 트리 연결 & 재귀함수

        void LinkHall()
        {
            for (int indx = 0; indx < halls.Count; indx++)
            {
                for (int i = 0; i < halls[indx].edgeRect.Count; i++)
                {
                    if (UtilityClass.CoinFlip(50) && halls[indx].isRoom ^ halls[indx].edgeRect[i].isRoom && (halls[indx].LinkedEdgeRect(halls[indx].edgeRect[i])))
                    {
                        DrawDoorTile(halls[indx], halls[indx].edgeRect[i]); //문 놓을 곳에 타일 지우기
                    }
                }
            }
        } // 복도랑 방문 연결

        void DrawDoorTile(Rect _rectA, Rect _rectB)
        {
            RuleTile verticalRuleTile = TileManager.Instance.verticalWallRuleTile;
            RuleTile horizonRuleTile = TileManager.Instance.horizonWallRuleTile;

            GameObject obj = null;
            if ((Mathf.Abs(_rectA.midX - _rectB.midX) == (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) < (float)(_rectA.height + _rectB.height) / 2))
            {
                List<int> yArr = new List<int>(4)
                {
                    _rectB.y * size,
                    (_rectB.y + _rectB.height) * size,
                    _rectA.y * size,
                    (_rectA.y + _rectA.height) * size
                };

                yArr.Sort();

                int interval = yArr[2] - yArr[1];
                int intervalNum = interval / size;

                int intervalResult = Random.Range(0, intervalNum) * size;
                int y = yArr[1] + intervalResult + size / 2;

                if (_rectA.midX > _rectB.midX) // 오른쪽 사각형이 메인
                {
                    verticalRuleTile.SetNull(new Vector3Int(_rectA.x * size, y, 0));
                    verticalRuleTile.SetNull(new Vector3Int(_rectA.x * size - 1, y, 0));
                    horizonRuleTile.SetNull(new Vector3Int(_rectA.x * size, y, 0));
                    horizonRuleTile.SetNull(new Vector3Int(_rectA.x * size - 1, y, 0));

                    if (_rectB.isRoom) // 왼쪽 방이  방임
                    {
                        obj = CreateDoorObject(_rectA.x * size + 0.84375f, y + 0.5f, true);
                    }
                    else
                    {
                        obj = CreateDoorObject(_rectA.x * size + 0.84375f, y + 0.5f, true);
                    }
                }
                else // 왼쪽 사각형이 메인
                {
                    verticalRuleTile.SetNull(new Vector3Int(_rectB.x * size, y, 0));
                    verticalRuleTile.SetNull(new Vector3Int(_rectB.x * size - 1, y, 0));
                    horizonRuleTile.SetNull(new Vector3Int(_rectB.x * size, y, 0));
                    horizonRuleTile.SetNull(new Vector3Int(_rectB.x * size - 1, y, 0));

                    if (_rectA.isRoom) // 오른쪽 방이 방임
                    {
                        obj = CreateDoorObject(_rectB.x * size + 0.84375f, y + 0.5f, true);
                    }
                    else
                    {
                        obj = CreateDoorObject(_rectB.x * size + 0.84375f, y + 0.5f, true);
                    }
                }
            } // 가로로 붙음
            else if ((Mathf.Abs(_rectA.midX - _rectB.midX) < (float)(_rectA.width + _rectB.width) / 2) && (Mathf.Abs(_rectA.midY - _rectB.midY) == (float)(_rectA.height + _rectB.height) / 2))
            {
                List<int> xArr = new List<int>(4)
                {
                    _rectB.x * size,
                    (_rectB.x + _rectB.width) * size,
                    _rectA.x * size,
                    (_rectA.x + _rectA.width) * size
                };

                xArr.Sort();

                int interval = xArr[2] - xArr[1];
                int intervalNum = interval / size;

                int intervalResult = Random.Range(0, intervalNum) * size;
                int x = xArr[1] + intervalResult + size / 2;

                if (_rectA.midY > _rectB.midY) // 위쪽
                {
                    verticalRuleTile.SetNull(new Vector3Int(x, _rectA.y * size, 0));
                    verticalRuleTile.SetNull(new Vector3Int(x, _rectA.y * size - 1, 0));
                    horizonRuleTile.SetNull(new Vector3Int(x, _rectA.y * size, 0));
                    horizonRuleTile.SetNull(new Vector3Int(x, _rectA.y * size - 1, 0));

                    obj = CreateDoorObject(x + 0.5f, _rectA.y * size - 0.5f, false);
                }
                else // 아래쪽
                {
                    verticalRuleTile.SetNull(new Vector3Int(x, _rectB.y * size, 0));
                    verticalRuleTile.SetNull(new Vector3Int(x, _rectB.y * size - 1, 0));
                    horizonRuleTile.SetNull(new Vector3Int(x, _rectB.y * size, 0));
                    horizonRuleTile.SetNull(new Vector3Int(x, _rectB.y * size - 1, 0));

                    obj = CreateDoorObject(x + 0.5f, _rectB.y * size - 0.5f, false);
                }

            } // 세로로 붙음

            _rectA.doorObjects.Add(obj.GetComponent<Door>());
            _rectB.doorObjects.Add(obj.GetComponent<Door>());

        } // Door 부분 타일 floor로 변경

        GameObject CreateDoorObject(float x, float y, bool isHorizon)
        {
            GameObject obj = objectPool.GetPooledObject();
            obj.AddComponent<Door>();
            obj.GetComponent<Door>().SetAxis(isHorizon);
            GameObject[] doorArrows = new GameObject[2];
            doorArrows[0] = Object.Instantiate(ResourceManager.Instance.DoorArrow);
            doorArrows[0].transform.parent = obj.transform;
            doorArrows[1] = Object.Instantiate(ResourceManager.Instance.DoorArrow);
            doorArrows[1].transform.parent = obj.transform;
            if (isHorizon)
            {
                doorArrows[0].transform.position = obj.transform.position - Vector3.right * .8f;
                doorArrows[1].transform.position = obj.transform.position + Vector3.right * .2f;
                doorArrows[1].transform.localScale *= -1;
                obj.GetComponent<Door>().Init(RoomSetManager.Instance.doorSprites[0], RoomSetManager.Instance.doorSprites[1], doorArrows);
            }
            else
            {
                doorArrows[0].transform.position = obj.transform.position - Vector3.up * .8f;
                doorArrows[0].transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
                doorArrows[1].transform.position = obj.transform.position + Vector3.up * .8f;
                doorArrows[1].transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
                doorArrows[1].transform.localScale *= -1;
                obj.GetComponent<Door>().Init(RoomSetManager.Instance.doorSprites[2], RoomSetManager.Instance.doorSprites[3], doorArrows);
            }
            obj.transform.localPosition = new Vector2(x, y);
            obj.GetComponent<SpriteRenderer>().sortingOrder = -Mathf.RoundToInt((y + 1) * 100);
            return obj;
        } // Door Object 생성

        void AssignAllRoom()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomSet roomSet = GetRoomSet(rooms[i].width, rooms[i].height);
                roomSet.x = rooms[i].x;
                roomSet.y = rooms[i].y;
                rooms[i].eRoomType = roomSet.roomType;
                rooms[i].gage = roomSet.gage;
                rooms[i].customObjects = AssignRoom(roomSet);
            }

            CreateStartPoint();
        } // 모든 룸 셋 배치

        void AssignAllHalls()
        {
            for (int i = 0; i < halls.Count; i++)
            {
                RoomSet roomSet = GetHallSet(halls[i].width, halls[i].height);
                if (roomSet == null)
                    continue;
                roomSet.x = halls[i].x;
                roomSet.y = halls[i].y;
                halls[i].eRoomType = roomSet.roomType;
                halls[i].gage = roomSet.gage;
                halls[i].customObjects = AssignRoom(roomSet);
            }
        }

        RoomSet GetRoomSet(int width, int height)
        {
            if (null == necessaryRoomSet)
                return RoomSetManager.Instance.LoadRoomSet(width, height); 
            if (necessaryRoomSet.Count == 0)
                return RoomSetManager.Instance.LoadRoomSet(width, height);
            else
            {
                RoomSet roomSet = RoomSetManager.Instance.LoadRoomSet(width, height);

                for (int i = 0; i < necessaryRoomSet.Count; i++)
                {
                    if (necessaryRoomSet[i].width == width && necessaryRoomSet[i].height == height)
                    {
                        roomSet = necessaryRoomSet[i];
                        settedRoomSet.Add(roomSet);
                        necessaryRoomSet.RemoveAt(i);
                        break;
                    }
                }

                return roomSet;
            }
        }

        RoomSet GetHallSet(int width, int height)
        {
            RoomSet roomSet = RoomSetManager.Instance.LoadHallSet(width, height);

            return roomSet;
        }

        GameObject[] AssignRoom(RoomSet _roomSet)
        {
            if (_roomSet == null)
                return null;
            List<GameObject> customObjects = new List<GameObject>(_roomSet.objectDatas.Count);
            int index = 0;

            for (int i = 0; i < _roomSet.objectDatas.Count; i++)
            {
                customObjects.Add(objectPool.GetPooledObject());
                customObjects[index].transform.localPosition = new Vector3(_roomSet.x * size + _roomSet.objectDatas[i].position.x, _roomSet.y * size + _roomSet.objectDatas[i].position.y, 0); //temp

                _roomSet.objectDatas[i].LoadObject(customObjects[index]);
                index++;
            }
            return customObjects.ToArray();
        } // 룸 셋 배치

        void BakeAvailableArea()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                AvailableAreas(rooms[i], 0.5f);
            }
        }

        void AvailableAreas(Rect _rect, float _radius)
        {
            _rect.DrawingArea(Color.red);
            Vector2 leftDown = _rect.areaLeftDown;
            Vector2 rightTop = _rect.areaRightTop;
            LayerMask layerMask = (1 << LayerMask.NameToLayer("TransparentFX"));
            float xGap = 0.25f;
            float yGap = 0.5f;
            for (float i = leftDown.x + xGap; i < rightTop.x - xGap; i += _radius)
                for (float j = leftDown.y + yGap; j < rightTop.y - yGap; j += _radius)
                    if (!Physics2D.OverlapCircle(new Vector2(i, j), _radius, layerMask))
                        _rect.availableAreas.Add(new Vector2(i, j));
        }

        void CreateStartPoint()
        {
            if (halls[0].width > halls[0].height)
                startPosition = new Vector3(halls[0].areaLeftDown.x + (halls[0].areaRightTop.x - halls[0].areaLeftDown.x) * 0.1f, (halls[0].areaLeftDown.y + halls[0].areaRightTop.y) / 2, 0);
            else
                startPosition = new Vector3((halls[0].areaLeftDown.x + halls[0].areaRightTop.x) / 2, halls[0].areaLeftDown.y + (halls[0].areaRightTop.y - halls[0].areaLeftDown.y) * 0.1f, 0);
        } // 스타트 포인트
        #endregion
    }

    public class BossRushMap : Map
    {
        public BossRushMap(int _width, int _height, int _max, int _mini, float _maxHallRate, ObjectPool _objectPool) : 
            base(_width, _height, _max, _mini, _maxHallRate, _objectPool)
        {

        }

        protected override void RectToBlock()
        {
            SplitHall(mainRect);
        }
        protected override void SplitHall(Rect _currentRect)
        {
            bool flag = true;
            Rect _blockA, _blockB;

            if (_currentRect.width > _currentRect.height)
                flag = true;
            else if (_currentRect.width < _currentRect.height)
                flag = false;
            else
            {
                if (UtilityClass.CoinFlip(50))
                    flag = true;
                else
                    flag = false;
            }

            if (flag) // 가로
            {
                int width;
                if (UtilityClass.CoinFlip(50))
                {
                    width = _currentRect.width - 1;
                    _blockA = new Rect(_currentRect.x, _currentRect.y, width, _currentRect.height, size);
                    _blockB = new Rect(_currentRect.x + width, _currentRect.y, _currentRect.width - width, _currentRect.height, size);
                    _blockB.isRoom = false;
                    _blockB.isClear = true;
                    _blockA.IsRoom();
                    halls.Add(_blockB);
                    rooms.Add(_blockA);
                }
                else
                {
                    width = 1;
                    _blockA = new Rect(_currentRect.x, _currentRect.y, width, _currentRect.height, size);
                    _blockB = new Rect(_currentRect.x + width, _currentRect.y, _currentRect.width - width, _currentRect.height, size);
                    _blockA.isRoom = false;
                    _blockA.isClear = true;
                    _blockB.IsRoom();
                    halls.Add(_blockA);
                    rooms.Add(_blockB);
                }
            }
            else
            {
                int height;

                if (UtilityClass.CoinFlip(50))
                {
                    height = _currentRect.height - 1;
                    _blockA = new Rect(_currentRect.x, _currentRect.y, _currentRect.width, height, size);
                    _blockB = new Rect(_currentRect.x, _currentRect.y + height, _currentRect.width, _currentRect.height - height, size);
                    _blockB.isRoom = false;
                    _blockB.isClear = true;
                    _blockA.IsRoom();
                    halls.Add(_blockB);
                    rooms.Add(_blockA);
                }
                else
                {
                    height = 1;
                    _blockA = new Rect(_currentRect.x, _currentRect.y, _currentRect.width, height, size);
                    _blockB = new Rect(_currentRect.x, _currentRect.y + height, _currentRect.width, _currentRect.height - height, size);
                    _blockA.isRoom = false;
                    _blockA.isClear = true;
                    _blockB.IsRoom();
                    halls.Add(_blockA);
                    rooms.Add(_blockB);
                }
            }
        }
        protected override bool BlockToRoom()
        {
            return true;
        }

    }

    public class Rect
    {
        #region parameter
        public int x
        {
            private set;
            get;
        }
        public int y
        {
            private set;
            get;
        }
        public int width
        {
            private set;
            get;
        }
        public int height
        {
            private set;
            get;
        }
        public int area
        {
            private set;
            get;
        }
        public float midX
        {
            private set;
            get;
        }
        public float midY
        {
            private set;
            get;
        }
        public readonly int size;
        #endregion
        #region variables
        public int gage;
        public bool isRoom;
        public bool downExist;
        public bool isClear;
        public bool isDrawed;
        public bool isLock;
        public bool visited;
        public RoomType eRoomType;
        #endregion
        #region dataStruct
        public Vector2 areaLeftDown, areaRightTop;
        public List<Rect> edgeRect;
        public List<Rect> linkedEdgeRect;
        public GameObject[] customObjects;
        public List<Door> doorObjects;
        public List<Vector2> availableAreas;

        #endregion
        #region getter
        public Vector3 GetAvailableArea()
        {
            return availableAreas[Random.Range(0, availableAreas.Count)]; ;
        }

        public Vector3 GetNearestAvailableArea(Vector2 position)
        {
            float dist = (position - availableAreas[0]).sqrMagnitude;
            float newDist = 0;
            Vector2 returnVector = availableAreas[0];
            for (int i = 0; i < availableAreas.Count; i++)
            {
                newDist = (position - availableAreas[i]).sqrMagnitude;
                if (newDist < dist)
                {
                    dist = newDist;
                    returnVector = availableAreas[i];
                }
            }
            return returnVector;
        }
        #endregion
        #region initialize
        public Rect(int _x, int _y, int _width, int _height, int _size)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
            area = width * height;
            midX = x + (float)width / 2;
            midY = y + (float)height / 2;
            size = _size;
            areaLeftDown = new Vector2(x * size + 0.5f, y * size + 0.5f);
            areaRightTop = new Vector2((x + width) * size + 0.5f, (y + height) * size + 0.5f);
            visited = false;
            downExist = false;
            isClear = false;
            edgeRect = new List<Rect>();
            linkedEdgeRect = new List<Rect>();
            doorObjects = new List<Door>();
            availableAreas = new List<Vector2>();
            isDrawed = false;
        }
        #endregion
        #region override
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Rect objAsPart = obj as Rect;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }

        public bool Equals(Rect other)
        {
            if (other == null) return false;
            if (this.x == other.x && this.y == other.y
                && this.width == other.width && this.height == other.height)
            {
                return true;
            }
            return false;
        }
        #endregion
        #region func
        public void IsRoom()
        {
            isRoom = true;
            areaLeftDown = new Vector2(x * size + 1, y * size + 1);
            areaRightTop = new Vector2((x + width) * size + 0, (y + height) * size);
        }

        public void EdgeRect(Rect _rect)
        {
            if (!edgeRect.Contains(_rect) && _rect != this)
            {
                edgeRect.Add(_rect);
                _rect.edgeRect.Add(this);
            }
        }

        public bool LinkedEdgeRect(Rect _rect)
        {
            if (!linkedEdgeRect.Contains(_rect) && _rect != this)
            {
                linkedEdgeRect.Add(_rect);
                _rect.linkedEdgeRect.Add(this);
            }
            else
                return false;
            return true;
        }

        public bool IsContain(Vector2 _position)
        {
            if (_position.x > areaLeftDown.x && _position.x < areaRightTop.x &&
                _position.y > areaLeftDown.y && _position.y < areaRightTop.y)
                return true;

            return false;
        }

        public bool Merge(ref List<Rect> rects, Rect rect, LinkedShape linkedShape)
        {
            if (linkedShape == LinkedShape.HORIZONTAL)
            {
                if(this.height == rect.height && this.y == rect.y)
                {
                    rects.Remove(rect);
                    rect.edgeRect.Remove(this);
                    this.edgeRect.Remove(rect);

                    for (int i = 0; i < rect.edgeRect.Count; i++)
                    {
                        if (!this.edgeRect.Contains(rect.edgeRect[i]))
                        {
                            this.edgeRect.Add(rect.edgeRect[i]);                  
                        }
                        rect.edgeRect[i].edgeRect.Remove(rect);
                        if(!rect.edgeRect[i].edgeRect.Contains(this))
                        {
                            rect.edgeRect[i].edgeRect.Add(this);
                        }
                    }
                    this.width += rect.width;
                    this.x = Mathf.Min(this.x, rect.x);
                    this.area += rect.area;
                    this.midX = x + (float)width / 2;
                    this.areaLeftDown = new Vector2(x * size + 0.5f, y * size + 0.5f);
                    this.areaRightTop = new Vector2((x + width) * size + 0.5f, (y + height) * size + 0.5f);

                    return true;
                }
                return false;
            }
            else if(linkedShape == LinkedShape.VERTICAL)
            {
                if(this.width == rect.width && this.x == rect.x)
                {
                    rects.Remove(rect);
                    rect.edgeRect.Remove(this);
                    this.edgeRect.Remove(rect);

                    for (int i = 0; i < rect.edgeRect.Count; i++)
                    {
                        if (!this.edgeRect.Contains(rect.edgeRect[i]))
                        {
                            this.edgeRect.Add(rect.edgeRect[i]);
                        }
                        rect.edgeRect[i].edgeRect.Remove(rect);
                        if (!rect.edgeRect[i].edgeRect.Contains(this))
                        {
                            rect.edgeRect[i].edgeRect.Add(this);
                        }
                    }
                    this.height += rect.height;
                    this.y = Mathf.Min(this.y, rect.y);
                    this.area += rect.area;
                    this.midY = y + (float)height / 2;
                    this.areaLeftDown = new Vector2(x * size + 0.5f, y * size + 0.5f);
                    this.areaRightTop = new Vector2((x + width) * size + 0.5f, (y + height) * size + 0.5f);

                    return true;
                }
                return false;
            }
            return false;
        }
        #endregion
        #region drawing
        public void Drawing(Color color, float offset)
        {
            Debug.DrawLine(new Vector3(x + offset, y) * size, new Vector3(x + offset, y + height) * size, color, 1000);
            Debug.DrawLine(new Vector3(x + offset, y + height) * size, new Vector3(x + offset + width, y + height) * size, color, 1000);
            Debug.DrawLine(new Vector3(x + offset + width, y + height) * size, new Vector3(x + offset + width, y) * size, color, 1000);
            Debug.DrawLine(new Vector3(x + offset + width, y) * size, new Vector3(x + offset, y) * size, color, 1000);

        }

        public void DrawingArea(Color color)
        {
            Debug.DrawLine(areaLeftDown, areaRightTop, color, 100);
        }
        #endregion
    }
}


