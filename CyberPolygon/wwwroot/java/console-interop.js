window.startVmConsole = function (wssUrl, containerId) {
    console.log(">_ Инициализация WMKS на: " + wssUrl);

    // Находим наш контейнер через jQuery
    var $container = $("#" + containerId);

    // Инициализируем консоль как jQuery UI виджет
    var wmks = $container.wmks({
        rescale: true,
        changeResolution: true,
        useNativeBpp: true
    });

    // В этой версии события привязываются через стандартный .bind()
    wmks.bind("wmksconnected", function () {
        console.log(">_ [SUCCESS] Канал связи с терминалом установлен.");
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