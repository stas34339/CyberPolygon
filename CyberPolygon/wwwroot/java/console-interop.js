window.startVmConsole = function (wssUrl, containerId) {
    console.log(">_ Инициализация WMKS на: " + wssUrl);

    // Находим наш контейнер через jQuery
    var $container = $("#" + containerId);

    // 1. ЖЕЛЕЗНАЯ ЗАЩИТА: Принудительно "запираем" контейнер
    // Это не даст абсолютно позиционированному canvas'у вылететь за края
    $container.css({
        "position": "relative",
        "overflow": "hidden"
    });

    // 2. Инициализируем консоль с правильными параметрами масштабирования
    var wmks = $container.wmks({
        rescale: true,
        changeResolution: true,
        useNativeBpp: true,
        fitToParent: true      // <-- ВАЖНО: Заставляет ВМ вписаться в размеры твоего <div>
    });
    // Функция для отправки любых комбинаций клавиш
    window.sendVmKeys = function (containerId, keyCodesArray) {
        var $container = $("#" + containerId);

        // Проверяем, инициализирован ли виджет
        if ($container.data("wmks-wmks")) {
            // Отправляем массив кодов клавиш в консоль
            $container.wmks("sendKeyCodes", keyCodesArray);
        } else {
            console.error(">_ [ERROR] WMKS еще не инициализирован.");
        }
    }
    // Функция для принудительного отжатия залипших модификаторов (Ctrl, Alt, Shift, Win)
    window.resetVmKeys = function (containerId) {
        var $container = $("#" + containerId);

        if ($container.data("wmks-wmks")) {
            // Отрицательные значения заставляют WMKS послать команду KeyUp (отпускание)[cite: 7]
            // 16 = Shift, 17 = Ctrl, 18 = Alt, 91 = Win
            $container.wmks("sendKeyCodes", [-16, -17, -18, -91]);
            console.log(">_ [INFO] Модификаторы клавиатуры принудительно отпущены.");
        }
    }

    // Привязываем события
    wmks.bind("wmksconnected", function () {
        console.log(">_ [SUCCESS] Канал связи с терминалом установлен.");

        // (Опционально) Фокусируемся на консоли после подключения, 
        // чтобы сразу можно было вводить пароль
        $container.focus();
    });

    wmks.bind("wmksdisconnected", function () {
        console.log(">_ [WARN] Канал связи разорван.");
    });

    wmks.bind("wmkserror", function (event, data) {
        console.error(">_ [ERROR] WMKS Ошибка: ", data);
    });

    // Запускаем подключение
    wmks.wmks("connect", wssUrl);
}