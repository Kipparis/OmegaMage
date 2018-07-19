using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : PT_MonoBehaviour {

// Публичные поля
    public string type;

// Скрытые приватные поля
    private string _tex;
    private int _height = 0;
    private Vector3 _pos;

// Свойства с get{} и set{}

    // Высота двигает плитку вверх и вниз. Стены имеют высоту 1
    public int height {
        get { return (_height); }
        set {
            _height = value;
            AdjustHeight(); // Пер: применить высоту
        }
    }

    // Задаёт текстуру плитки в зависимости от строки
    // Для этого нужно LayoutTiles, так что закоментим пока
    public string tex {
        get { return (_tex); }
        set {
            _tex = value;
            name = "TilePrefab_" + _tex;    // Задаём имя этого ио
            Texture2D t2D = LayoutTiles.S.GetTileTex(_tex);
            if (t2D == null) {
                Utils.tr("ERROR", "Tile.type{set}=", value, "No mathing Texture2D in LayoutTiles.S.tileTextures!");
            } else {
                GetComponent<Renderer>().material.mainTexture = t2D;
            }
        }
    }

    // Используем слово "new" чтобы заменить pos наследуемую из PT_MonoBehabior
    // Без этого слова два свойства будут конфликтовать
    new public Vector3 pos {
        get { return (_pos); }
        set {
            _pos = value;
            AdjustHeight(); // Пер: применить высоту
        }
    }

// Методы
    public void AdjustHeight() {
        // Двигаем блок вверх или вниз в зависимости от _height
        Vector3 vertOffset = Vector3.back * (_height - 0.6f);
        // Сдвиг на -0.5f делает так, что при высоте 0, верхняя плоскость кубов будет на z высоте 0
        transform.position = _pos + vertOffset;
    }
}
