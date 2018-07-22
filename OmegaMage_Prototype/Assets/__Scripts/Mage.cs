using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// MPhase используется чтобы отслеживать фазу взаимодействия с мышкой
public enum MPhase {
    idle,
    down,
    drag
}

public enum ElementType {
    earth,
    water,
    air,
    fire,
    aether,
    none
}

// MouseInfo записывает информацию о мышке в каждый кадр взаимодействия
[System.Serializable]
public class MouseInfo {
    public Vector3 loc; // 3D позиция мыши рядом с z = 0
    public Vector3 screenLoc;   // Позиция мышки на экране
    public Ray ray; // Луч от мышки в 3D мир
    public float time;  // Время когда инфа была записанна
    public RaycastHit hitInfo;  // Информация о том что луч встретил на пути
    public bool hit;    // Была ли мыш над каким то коллайдером

    // Эти методы видят ударил ли mouseRay что нибудь
    public RaycastHit Raycast() {
        hit = Physics.Raycast(ray, out hitInfo);
        return (hitInfo);
    }

    public RaycastHit Raycast(int mask) {
        hit = Physics.Raycast(ray, out hitInfo, mask);
        return (hitInfo);
    }
}

//  Маг наследует силу от книжечки
public class Mage : PT_MonoBehaviour {
    static public Mage S;
    static public bool DEBUG = true;

    public float mTapTime = 0.1f;   // Как долго должно быть нажатие
    public float mDragDist = 5; // Минимальная дистанция в пикселях чтобы перемещать
    public GameObject tapIndicatorPrefab;   // Префаб индикатора нажатия

    public float activeScreenWidth = 1; // % экрана чтобы использовать

    public float speed = 2f;    // Скорость с которой _Mage волкин

    public GameObject[] elementPrefabs; // Сферы элементов
    public float elementRotDist = 0.5f; // Радиус вращения
    public float elementRotSpeed = 0.5f;    // Период вращение
    public int maxNumSelectedElements = 1;
    public Color[] elementColors;

    // Задаёт минимальное и максимальное расстояние между двумя точками линии
    public float lineMinDelta = 0.1f;
    public float lineMaxDelta = 0.5f;
    public float lineMaxLength = 8f;

    public GameObject fireGroundSpellPrefab;

    public float health = 4;    // Хп мага
    public float damageTime = -100;
    public float knockbackDist = 1; // Дистанция для отброса
    public float knockbackDur = 0.5f;   // Секунд чтобы двигаться обратно
    public float invincibleDur = 0.5f;  // Секунд чтобы быть неуязвимым
    public int invTimesToBlink = 4; // кол-во мигания пока неуязвим

    public bool _____________________;

    private bool invincibleBool = false;    // Маг неуязвим?
    private bool knockbackBool = false; // Маг отброшен?
    private Vector3 knockbackDir;   // Направление отброса
    private Transform viewCharacterTrans;

    protected Transform spellAnchor;    // Родитель для всех скиллов

    public float totalLineLength;

    public List<Vector3> linePts;   // Точки которые будет показывать линия
    protected LineRenderer liner;   // Ссылка на компонент лайн рендерер
    protected float lineZ = -0.1f;  // Z глубина линии
    // ^ Щашишённые переменные может видеть только класс и наследуемые классы
    // только публичные переменные появляются в инспекторе
    // или те что помеченны [SerializeField] 

    public MPhase mPhase = MPhase.idle;
    public List<MouseInfo> mouseInfos = new List<MouseInfo>();
    public string actionStartTag;   // ["Mage","Ground","Enemy"]

    public bool walking = false;
    public Vector3 walkTarget;
    public Transform characterTrans;

    public List<Element> selectedElements = new List<Element>();

    private void Awake() {
        S = this;   // Задаём синглтон как грица
        mPhase = MPhase.idle;

        // Находим characterTrans, чтобы поворачивать с помощью Face()
        characterTrans = transform.Find("CharacterTrans");
        viewCharacterTrans = characterTrans.Find("View_Character");

        // Получаем компонент LineRenderer
        liner = GetComponent<LineRenderer>();
        liner.enabled = false;

        GameObject saGO = new GameObject("SpellAnchor");
        // ^ когда создаём новый ио, он становится в позицию 000 000 111
        spellAnchor = saGO.transform;
    }

    void Update() {
        // Узнаём была ли нажата или отпущена левая кнопка в этот кадр
        bool b0Down = Input.GetMouseButton(0);
        bool b0Up = Input.GetMouseButtonUp(0);

        // Обрабатываем здесь ввод ( кроме инвентаря )
        /*
        Здесь только несколько возможных действий    
        1. Нажимать на экран чтобы перемещаться в эту точку
        2. Перемещаем землю с невыбранной способностью чтобы перемещать мага
        3. Растягиваем по земле с выбранной способностью что скастовать её
        4. Жмём на врага чтобы атаковать
         */

        // Пример использования < чтобы вернуть булевое значение
        bool inActiveArea = (float)Input.mousePosition.x / Screen.width < activeScreenWidth;

        // Обрабатывается с if условием вместо switch, потому что нажатие иногда может происходить в 1 кадр
        if (mPhase == MPhase.idle) { // Если мышка покоится
            if (b0Down && inActiveArea) {
                mouseInfos.Clear(); // Очищаем список MouseInfo
                AddMouseInfo(); // Добавляем первое MouseInfo

                // Если мышка указывала на что-то, это подходящее нажатие
                if (mouseInfos[0].hit) { // Что то нажалось
                    MouseDown();    // Вызываем MouseDown()
                    mPhase = MPhase.down;   // Задаём фазу
                }
            }
        }


        if (mPhase == MPhase.down) { // Если мышка опущена
            AddMouseInfo(); // Добавляем MouseInfo за этот кадр
            if (b0Up) { // Если мышка была отпущена
                MouseTap(); // Это было нажатие
                mPhase = MPhase.idle;
            } else if (Time.time - mouseInfos[0].time > mTapTime) {
                // Длилось дольше чем просто нажатие, так что это должно быть перемещение
                // Но для перемещения также должно быть пройденно определённое кол-во пикселей
                float dragDist = (lastMouseInfo.screenLoc - mouseInfos[0].screenLoc).magnitude;
                if (dragDist >= mDragDist) {
                    mPhase = MPhase.drag;
                }

                // Драг будет сразу начинается после прошедшего времени если нет выбранных элементов
                if (selectedElements.Count == 0) {
                    mPhase = MPhase.drag;
                }
            }
        }

        if (mPhase == MPhase.drag) { // Мыш (кродётся)
            AddMouseInfo();
            if (b0Up) {
                // Мышь была отпущенна
                MouseDragUp();
                mPhase = MPhase.idle;
            } else {
                MouseDrag();    // Всё ещё перетаскиваем
            }
        }

        OrbitSelectedElements();
    }

    // Закидывает инфу о мышке, добавляет её список и возвращает
    MouseInfo AddMouseInfo() {
        MouseInfo mInfo = new MouseInfo();
        mInfo.screenLoc = Input.mousePosition;
        mInfo.loc = Utils.mouseLoc; // Получаем позицию мышки в z = 0
        mInfo.ray = Utils.mouseRay; // Кастуем луч из Main Camera через точку мышки

        mInfo.time = Time.time;
        mInfo.Raycast();    // Базовый рэйкаст без маски

        if (mouseInfos.Count == 0) {
            // Это первый mouseInfo
            mouseInfos.Add(mInfo);  // Добавляем новый элемент
        } else {
            float lastTime = mouseInfos[mouseInfos.Count - 1].time;
            if (mInfo.time != lastTime) {   // Защита от того чтобы одна и та же точка не добавилась дважды
                mouseInfos.Add(mInfo);  // Добавляем
            }
        }
        return (mInfo);
    }

    public MouseInfo lastMouseInfo {
        // Выбираем последнюю mouseInfo
        get {
            if (mouseInfos.Count == 0) return null;
            return (mouseInfos[mouseInfos.Count - 1]);
        }
    }

    void MouseDown() {
        // Если мышка была нажата на чём то, это может быть просто нажатие или перемещение
        if (DEBUG) print(gameObject.name + ":Mage.MouseDown()");

        GameObject clickedGO = mouseInfos[0].hitInfo.collider.gameObject;
        // ^ если мышка ничего не нажала это вызовет ошибку
        // однако мы знаем что MouseDOwn() вызывается только когда кликаем на что-то

        GameObject taggedParent = Utils.FindTaggedParent(clickedGO);
        if (taggedParent == null) {
            actionStartTag = "";
        } else {
            actionStartTag = taggedParent.tag;
            // ^ Это либо земля, либо маг, либо враг
        }
    }

    void MouseTap() {
        // Чтото было нажато как на кнопку
        if (DEBUG) print(gameObject.name + ":Mage.MouseTap()");

        // На что было нажато
        switch (actionStartTag) {
            case "Mage":
                // Ничего не делаем
                break;
            case "Ground":
                WalkTo(lastMouseInfo.loc);  // Идём к последней точке mouseInfo
                ShowTap(lastMouseInfo.loc); // Показываем где игрок нажал
                break;
        }
    }

    void MouseDrag() {
        // Мыш ( кродётся )
        if (DEBUG) print(gameObject.name + ":Mage.MouseDrag()");

        // Перемещение бесполезно, до тех пор пока мышка не начинает на земле
        if (actionStartTag != "Ground") return;
        
        // Еслин не было выбранно элементов, игрок просто следует мышке
        if (selectedElements.Count == 0) {
            // Продолжительно идём навстречу текущей mouseInfo позиции
            WalkTo(mouseInfos[mouseInfos.Count - 1].loc);
        } else {
            // Это ground spell, так что нам надо нарисовать линию
            AddPointToLiner(mouseInfos[mouseInfos.Count - 1].loc);
            // ^ добавляем последнюю инфу о мышке к линии
        }
    }

    void MouseDragUp() {
        // Мышка была отпущенна после перемещения
        if (DEBUG) print(gameObject.name + ":MageMouseDragUp()");

        // Перемещение бесполезно, до тех пор пока мы не на земле
        if (actionStartTag != "Ground") return;

        // Если нет выбранных элементов, перестаём двигаться
        if(selectedElements.Count == 0) {
            // Перестаём двигаться когда перетаскивание законченно
            StopWalking();
        } else {
            CastGroundSpell();

            // Обнуляем линию
            ClearLiner();
        }
    }

    void CastGroundSpell() {
        // Если нет выбранного элемента, возвращаем
        if (selectedElements.Count == 0) return;

        switch (selectedElements[0].type) {
            case ElementType.fire:
                GameObject fireGO;
                foreach (Vector3 pt in linePts) {
                    // Создаём новый огонёк
                    fireGO = Instantiate(fireGroundSpellPrefab) as GameObject;
                    fireGO.transform.parent = spellAnchor;
                    fireGO.transform.position = pt;
                }
                break;
                //TODO: ДОбавить другие элементы
        }
    }

    // Идём к определённой позиции. Координата z всегда 0
    public void WalkTo(Vector3 xTarget) {
        walkTarget = xTarget;   // Задаём позицию куда гуляем
        walkTarget.z = 0;   // z = 0
        walking = true; // Сейчас маг гуляет
        Face(walkTarget);    // Смотрим в направлении walkTarget
    }

    public void Face(Vector3 poi) { // Разворачиваемся к точке интереса
        Vector3 delta = poi - pos;
        // Используем Atan2 чтобы получить поворот вокруг Z который указывает на х осьот _Mage:CharacterTrans
        float rZ = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
        // Задаём поворот characterTrans ( на самом деле не поворачивает _Mage )
        characterTrans.rotation = Quaternion.Euler(0, 0, rZ);
    }

    public void StopWalking() { // Останавливает мага от хождения
        walking = false;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    void FixedUpdate() {    // Случается каждый шаг когда считается физика ( 50 /с-1 )
        if (invincibleBool) {
            // Достаём номер от 0 - 1
            float blinkU = (Time.time - damageTime) / invincibleDur;
            blinkU *= invTimesToBlink;  //  Умножаем на количество миганий
            blinkU %= 1f;   // Даёт числа после запятой
            bool visible = (blinkU > 0.5f);
            if (Time.time - damageTime > invincibleDur) {
                invincibleBool = false;
                visible = true; // На всякий пожарный
            }
            // Делание ио неактивным делает его невидимым
            viewCharacterTrans.gameObject.SetActive(visible);
        }
        if (knockbackBool) {
            if (Time.time - damageTime > knockbackDur) {
                knockbackBool = false;
            }
            float knockbackSpeed = knockbackDist / knockbackDur;
            vel = knockbackDir * knockbackSpeed;
            return; // Возвращаем чтобы избежать кода для бега ниже
        }
        if (walking) { // Если маг гуляет
            if ((walkTarget - pos).magnitude < Time.fixedDeltaTime * speed) {
                // Если маг очень близок к цели, просто останавливаемся
                pos = walkTarget;
                StopWalking();
            } else {
                // Движемся навстречу walkTarget
                GetComponent<Rigidbody>().velocity = (walkTarget - pos).normalized * speed;
            }
        } else {
            // Если не волкин, скорость равна нулю
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        GameObject otherGO = collision.gameObject;

        // Стык со стенами также останавливает движение
        Tile ti = otherGO.GetComponent<Tile>();
        if (ti != null) {
            if (ti.height > 0) {
                // Значит это стена и маг должен остановится
                StopWalking();
            }
        }

        // Чекаем врага
        EnemyBug bug = collision.gameObject.GetComponent<EnemyBug>();
        // Если рил павук, зовём помощь
        // Если это паук, передаём его, чтобы он обрабатывался как враг
        if (bug != null) CollisionDamage(bug);
        //if (bug != null) CollisionDamage(otherGO);
    }

    private void OnTriggerEnter(Collider other) {
        EnemySpiker spiker = other.GetComponent<EnemySpiker>();
        if (spiker != null) {
            // CollisionDamage() будет видеть спираль как врага
            CollisionDamage(spiker);
            //CollisionDamage(other.gameObject);
        }
    }

    void CollisionDamage(Enemy enemy) {

        // Не получаем урон если неуязвимы
        if (invincibleBool) return;

        // Маг был ударен врагом
        StopWalking();
        ClearInput();

        health -= enemy.touchDamage;    // Убираем хпшечку, в зависимости от определённого
        if (health <= 0) {
            Die();
            return;
        }

        damageTime = Time.time;
        knockbackBool = true;
        knockbackDir = (pos - enemy.pos).normalized;
        invincibleBool = true;
    }

    // Маг умирает T_T
    void Die() {
        Application.LoadLevel(0);
    }

    // Показываем где игрок нажал
    public void ShowTap(Vector3 loc) {
        GameObject go = Instantiate(tapIndicatorPrefab) as GameObject;
        go.transform.position = loc;
    }

    // Выбираем элемент_сферу с переданным типом и добавляем её в selectedElements
    public void SelectElement(ElementType elType) {
        if (elType == ElementType.none) {   // Если был передан тип none
            ClearElements();    // Очищаем все элементы
            return;
        }

        if (maxNumSelectedElements == 1) {
            // Если только 1 может быть выбран, очищаем его
            ClearElements();
        }

        // Не можем выбрать больше чем maxNumSelectedElements
        if (selectedElements.Count >= maxNumSelectedElements) return;

        // Всё нормально, можно добавлять элемент
        GameObject go = Instantiate(elementPrefabs[(int)elType]) as GameObject;
        Element el = go.GetComponent<Element>();
        el.transform.parent = this.transform;

        selectedElements.Add(el);   // Добавляем элемент в выбранные элементы
    }

    public void ClearElements() {
        foreach (Element el in selectedElements) {
            // Уничтожаем каждый ио в списке
            Destroy(el.gameObject);
        }
        selectedElements.Clear();   // Очищаем список
    }

    // Вызывается каждый Update() чтобы вращать элементы вокруг
    void OrbitSelectedElements() {
        // Если нет выбранных, просто возвращаем
        if (selectedElements.Count == 0) return;

        Element el;
        Vector3 vec;
        float theta0, theta;
        float tau = Mathf.PI * 2;   // тау это 360 градусов в радианах (примерно 6,283)

        // Делим круг на кол-во элементов которые будут крутиться
        float rotPerElement = tau / selectedElements.Count;

        // Базовое врощение, основывается на времени
        theta0 = elementRotSpeed * Time.time * tau;

        for (int i = 0; i < selectedElements.Count; i++) {
            // Определяем поворот для каждого элемента
            theta = theta0 + i * rotPerElement;
            el = selectedElements[i];
            // Используем простую тригонометрию чтобы превратить поворот в вектор
            vec = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0);
            // Умножаем единичный вектора на elementRotDist
            vec *= elementRotDist;
            // Поднимаем элемент
            vec.z = -0.5f;
            el.lPos = vec;  // Задаём позицию Element_Sphere
        }
    }

    //----------------LineRenderer Code --------------//

    // Добавляем новую точку к линии. Игнорирует точку если она слишком близко,
    // добавляет новую, если слишком далеко
    void AddPointToLiner(Vector3 pt) {
        pt.z = lineZ;   // Задаём Z позицию чтоб линия была немного выше земли
        //linePts.Add(pt);
        //UpdateLiner();

        // Всегда добавляем точку, если linePts пуст
        if (linePts.Count == 0) {
            linePts.Add(pt);
            totalLineLength = 0;
            return;
        }

        // Если линия уже слишком длинная, возвращаем
        if (totalLineLength > lineMaxLength) return;

        // Если есть предыдущая точка ( pt0 ), узнаём как она далеко
        Vector3 pt0 = linePts[linePts.Count - 1];   // Находим последнюю точку
        Vector3 dir = pt - pt0;
        float delta = dir.magnitude;
        dir.Normalize();

        // Если она меньше чем минимальная дистанция
        if (delta < lineMinDelta) {
            // Слишком близко, так что не добавляем
            return;
        }

        // Если дальше чем максимальная дистанция, добавляем точку
        if (delta > lineMaxDelta) {
            // Посередине мужду ними
            float numToAdd = Mathf.Ceil(delta / lineMaxDelta);
            float midDelta = delta / numToAdd;
            Vector3 ptMid;
            for (int i = 1; i < numToAdd; i++) {
                ptMid = pt0 + (dir * midDelta * i);
                linePts.Add(ptMid);
            }
        }

        totalLineLength += delta;
        linePts.Add(pt);    // Добавляем точку
        UpdateLiner();  // Наконец обновляем линию
    }

    // Обновляем лайн рендерер со всеми новыми точками
    public void UpdateLiner() {
        // Получаем тип выбранного элемента
        int el = (int)selectedElements[0].type;

        // Задаём цвет линии
        liner.endColor = liner.startColor = elementColors[el];

        // Обновляем репрезентацию о наземном скилле
        liner.SetVertexCount(linePts.Count);    // задаём кол-во вершин
        for (int i = 0; i < linePts.Count; i++) {
            liner.SetPosition(i, linePts[i]);   // Задаём каждую вершину
        }
        liner.enabled = true;
    }

    public void ClearLiner() {
        liner.enabled = false;  // Выключаем лайн рендерер
        linePts.Clear();    // очищаем linePts список
    }

    // Прекращаем любое активное нажатие
    public void ClearInput() {
        mPhase = MPhase.idle;
    }
}
