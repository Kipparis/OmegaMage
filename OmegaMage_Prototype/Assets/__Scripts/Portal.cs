using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : PT_MonoBehaviour {

    public string toRoom;
    public bool justArrived = false;
    // ^ истина если маг только что телепортировался сюда

    void OnTriggerEnter(Collider other) {
        if (Mage.DEBUG) Utils.tr("Portal collide with comething: ", other.gameObject);
        if (justArrived) return;    // Т.к. маг только телепортировался не возвращаем его обратно

        // Получаем ио коллайдера
        GameObject go = other.gameObject;
        // Ищем родителя с тэгом
        GameObject goP = Utils.FindTaggedParent(go);
        if (goP != null) go = goP;

        // Если это не маг возвращаем
        if (go.tag != "Mage") return;

        // Идём дальше, и билдим следующую комнату
        LayoutTiles.S.BuildRoom(toRoom);
    }

    void OnTriggerExit(Collider other) {
        // Как только маг вышел, ставим фолс
        if (other.gameObject.tag == "Mage") {
            justArrived = false;
        }
    }
}
