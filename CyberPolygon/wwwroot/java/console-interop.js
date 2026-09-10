window.startVmConsole = function (wssUrl, containerId) {
    console.log(">_ Инициализация WMKS на: " + wssUrl);

    // Находим наш контейнер
    var $container = $("#" + containerId);

    // 1. ЖЕЛЕЗНАЯ ЗАЩИТА: Принудительно "запираем" контейнер
    $container.css({
        "position": "relative",
        "overflow": "hidden"
    });

    // 2. Инициализируем консоль с правильными параметрами
    var wmks = $container.wmks({
        rescale: true,
        changeResolution: true,
        useNativeBpp: true,
        fitToParent: true,
        disableVscanKeyboard: false // <--- КРИТИЧЕСКИ ВАЖНЫЙ ФИКС ДЛЯ РУССКОГО ЯЗЫКА И БУКВ
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
        var widget = $container.data("wmks-wmks");

        if (widget) {
            // 1. Очищаем внутренние массивы зажатых клавиш в самом WMKS
            if (widget._keyboardManager) {
                if (typeof widget._keyboardManager.cancelModifiers === 'function') {
                    widget._keyboardManager.cancelModifiers(true);
                }
                if (typeof widget._keyboardManager.clearState === 'function') {
                    widget._keyboardManager.clearState();
                }
            }

            // 2. Хардкорно отправляем VScan-коды отпускания (KeyUp = false) в виртуальную машину
            var decoder = widget._vncDecoder;
            if (decoder && typeof decoder.onKeyVScan === 'function') {
                decoder.onKeyVScan(0x02A, false); // Левый SHIFT
                decoder.onKeyVScan(0x036, false); // Правый SHIFT
                decoder.onKeyVScan(0x01D, false); // Левый CTRL
                decoder.onKeyVScan(0x11D, false); // Правый CTRL
                decoder.onKeyVScan(0x038, false); // Левый ALT
                decoder.onKeyVScan(0x138, false); // Правый ALT
                decoder.onKeyVScan(0x15B, false); // Левый WIN
                decoder.onKeyVScan(0x15C, false); // Правый WIN
            }

            console.log(">_ [INFO] Модификаторы клавиатуры принудительно отпущены.");

            // 3. Возвращаем фокус ввода обратно на холст виртуальной машины!
            var canvas = $container.find("canvas").get(0);
            if (canvas) {
                canvas.focus();
            } else {
                $container.focus();
            }
        } else {
            console.error(">_ [ERROR] WMKS еще не инициализирован.");
        }
    }

    // Привязываем события
    wmks.bind("wmksconnected", function () {
        console.log(">_ [SUCCESS] Канал связи с терминалом установлен.");
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