(function($) {
    $.fn.longAndShortClick = function(callbackLongclick, callbackNormalClick, timeout) {
		var timerExpired = false;
        var timer;
        timeout = timeout || 1000;
        $(this).mousedown(function()
		{
			var ele = this;
			timerExpired = false;
            timer = setTimeout(function() { timerExpired = true; callbackLongclick(ele); }, timeout);
            return false;
        });
        $(this).mouseout(function()
		{
            clearTimeout(timer);
            return false;
		});
        $(this).mouseup(function()
		{
            clearTimeout(timer);
			if(!timerExpired)
				callbackNormalClick(this);
            return false;
        });
    };

})(jQuery);