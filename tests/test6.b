proc f(a:Int):Int {
    var i = 0;
    while i < a do {
        var j = 0;
        while j < a do {
            print i + j;
            j = j + 1;
        };
        i = i + 1;
    };
    return 0;
}

start {
    print f(2);
    return 0;
}