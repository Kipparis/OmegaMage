﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileTex {
    // Класс позволяет определить разные текстуры для плиток
    public string str;
    public Texture2D tex;
}

[System.Serializable]
public class EnemyDef {
    // Позволяет определить несколько врагов
    public string str;
    public GameObject go;
}

public class LayoutTiles : MonoBehaviour {
    static public LayoutTiles S;

    public TextAsset roomsText; // Rooms.xml файл
    public string roomNumber = "0"; // Текущий номер комнаты как строка
    // ^ roomNumber как строка позволяет кодировать XML и комнаты 0-F
    public GameObject tilePrefab;   // Префаб для всех плит
    public TileTex[] tileTextures;  // Список именнованных текстур для плиток

    public GameObject portalPrefab; // Префаб портала между комнатами
    public EnemyDef[] enemyDefinitions; // Префабы для врагов

    public bool _____________________;

    private bool firstRoom = true;  // Это первая комната?

    public PT_XMLReader roomsXMLR;
    public PT_XMLHashList roomsXML;
    public Tile[,] tiles;
    public Transform tileAnchor;

    void Awake() {
        S = this;   // Задаём синглтон

        // Создаём новый ио чтобы быть якорем для всех плит
        // сохранит нашу иерархию чистой
        GameObject tAnc = new GameObject("TileAnchor");
        tileAnchor = tAnc.transform;

        // Читаем XML
        roomsXMLR = new PT_XMLReader(); // Создаём считыватель
        roomsXMLR.Parse(roomsText.text);    // Передаём Rooms.xml
        roomsXML = roomsXMLR.xml["xml"][0]["room"]; // Достаём все комнаты
    }

    void Start() {
        // Создаём нулевую ( первую ) комнату
        BuildRoom(roomNumber);
    }

    // Метод который использует класс Tile
    public Texture2D GetTileTex(string tStr) {
        // Ищем во всех tileTextures корректную строку
        foreach (TileTex tt in tileTextures) {
            if (tt.str == tStr) {
                return (tt.tex);
            }
        }
        // Возвращаем null если ничего не найденно
        return (null);
    }

    // Строим комнату основанную на room number. Альтернативная версия BuildRoom
    // которая выбирает roomXML основываясь на room num
    public void BuildRoom(string str) {
        PT_XMLHashtable roomHT = null;
        for (int i = 0; i < roomsXML.Count; i++) {
            roomHT = roomsXML[i];
            if (roomHT.HasAtt("num")) {
                if (roomHT.att("num") == str) {
                    BuildRoom(roomHT);
                    return;
                }
            }
        }
        Utils.tr("ERROR", "LayoutTiles.BuildRoom()", "Room not found: ", str);
    }

    public void BuildRoom(PT_XMLHashtable room) {
        // Разрушаем старые плитки
        foreach (Transform t in tileAnchor) {
            Destroy(t.gameObject);
        }

        // Двигаем мага подальше
        Mage.S.pos = Vector3.left * 1000;   // Чтобы случайно не затригерил ничего
        Mage.S.ClearInput();    // Очищаем любые фигнюшки на маге

        string rNumStr = room.att("num");
        // Выбираем текстуры для поля и для стен
        string floorTexStr = room.att("floor");
        string wallTexStr = room.att("wall");

        // Делим комнату на строки, основываясь на возврат каретки в Room.xml
        string[] roomRows = room.text.Split('\n');
        // Убираем табы, но оставляем пробелы и нижние подчеркивания для непрямоугольных комнат
        for (int i = 0; i < roomRows.Length; i++) {
            roomRows[i] = roomRows[i].Trim('\t');
        }

        // Очищаем массив плит
        tiles = new Tile[100, 100]; // Произвольный размер комнаты

        // Объявляем некоторые локальные поля, которые будут использоваться позже
        Tile ti;
        string type, rawType, tileTexStr;
        GameObject go;
        int height;
        float maxY = roomRows.Length - 1;
        List<Portal> portals = new List<Portal>();

        // Этот цикл сканирует в каждой строчке каждую плитку
        for (int y = 0; y < roomRows.Length; y++) {
            for (int x = 0; x < roomRows[y].Length; x++) {
                // Задаём базовые значения
                height = 0;
                tileTexStr = floorTexStr;

                // Достаём символ, который определяет плитку
                type = rawType = roomRows[y][x].ToString();
                switch (rawType) {
                    case " ":   // Пустой пробел
                    case "_":   // Пустой пробел
                        // Просто пропускаем
                        continue;
                    case ".":   // Базовый пол
                        // Сохраняем тип = "."
                        break;
                    case "|":   // Базовая стена
                        height = 1;
                        break;
                    default:    // Что то другое будет рассматриваться как пол
                        type = "."; // rawType будет какой-то финтифлюшкой, а этот тип будет похож на стену
                        break;  // таким образом автор потом различит объекты - не интерьер
                }

                // Задаём текстуру для пола или стены основываясь на аттрибутах
                if (type == ".") {
                    tileTexStr = floorTexStr;
                } else if (type == "|") {
                    tileTexStr = wallTexStr;
                }

                // Создаём новый TilePrefab
                go = Instantiate(tilePrefab) as GameObject;
                ti = go.GetComponent<Tile>();
                // Задаём родителя
                ti.transform.parent = tileAnchor;
                // Задаём позицию 
                ti.pos = new Vector3(x, maxY - y, 0);
                tiles[x, y] = ti;   // Добавляем плитку в наш массив

                // Задаём тип, высоту и текстуру
                ti.type = type;
                ti.height = height;
                ti.tex = tileTexStr;

                // Если тип до сих пор равен rawType, идём к след. итерации
                if (rawType == type) continue;

                // Проверяем специфичные создания в комнате
                switch (rawType) {
                    case "X":   // Стартовая позиция для мага
                        //Mage.S.pos = ti.pos;
                        if (firstRoom) {
                            Mage.S.pos = ti.pos;    // Используем синглтон мага
                            roomNumber = rNumStr;
                            firstRoom = false;
                        }
                        break;
                    case "0":   // Номера это порталы в комнату
                    case "1":   // Позволяет распологать порталыв Rooms.xml
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                    case "A":
                    case "B":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                        // Создаём портал
                        GameObject pGO = Instantiate(portalPrefab) as GameObject;
                        // Когда комната сменится, портал тоже удалится
                        pGO.transform.parent = tileAnchor;
                        Portal p = pGO.GetComponent<Portal>();
                        p.pos = ti.pos;
                        p.toRoom = rawType;
                        pGO.GetComponent<Collider>().isTrigger = false;
                        pGO.GetComponent<Collider>().isTrigger = true;
                        portals.Add(p);
                        break;
                    default:
                        // Ищем есть ли враг для этой буквы
                        Enemy en = EnemyFactory(rawType);
                        if (en == null) break;  // Если нет подходящего, возвращаем
                        // Устанавливаем врага
                        en.pos = ti.pos;
                        // Делаем врага дитём tileAnchor так он удалится на след.комнате
                        en.transform.parent = tileAnchor;
                        en.typeString = rawType;
                        break;
                }

                // Дальше лучше :)
            }
        }

        // Располагаем мага
        foreach (Portal p in portals) {
            if (p.toRoom == roomNumber || firstRoom) {
                Mage.S.StopWalking();   // Прекращаем любое движение мага
                Mage.S.pos = p.pos;
                p.justArrived = true;
                firstRoom = false;
            }
        }

        roomNumber = rNumStr;
    }

    public Enemy EnemyFactory(string typeStr) {
        GameObject prefab = null;
        foreach (EnemyDef ed in enemyDefinitions) {
            if (ed.str == typeStr) {
                prefab = ed.go;
                break;
            }
        }
        if (prefab == null) {
            Utils.tr("LayoutTiles.EnemyFactory()", "No EnemyDef for: " + typeStr);
            return (null);
        }

        GameObject go = Instantiate(prefab) as GameObject;
        // Обычная форма GetComponent<>() не работает с интерфейсами, 
        // Так что делаем по другому
        Enemy en = (Enemy)go.GetComponent(typeof(Enemy));

        return (en);
    }
}
