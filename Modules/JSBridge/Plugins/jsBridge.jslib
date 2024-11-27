mergeInto(LibraryManager.library, {

    SendMessageFromUnity: function (arg) {
        var message = UTF8ToString(arg);
        console.log(JSON.stringify(message));
        
        try 
        {
            window.UnitySendMessage(message);
        }
        catch (error)
        {
            console.log(error);
        }
    },

    ReceiveMock: function (arg) {
        var message = UTF8ToString(arg);
        console.log("Mock [JS -> UNITY] message");
        console.log(JSON.stringify(message));
        unityInstance.SendMessage('Js_Bridge', 'InvokeReceiveMessage', message);
    }
});