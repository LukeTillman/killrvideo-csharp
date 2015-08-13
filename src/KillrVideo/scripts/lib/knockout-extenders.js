define(["knockout"], function(ko) {
    // An async extender, taken from http://smellegantcode.wordpress.com/2012/12/10/asynchronous-computed-observables-in-knockout-js/
    ko.extenders.async = function (computedDeferred, initialValue) {

        var plainObservable = ko.observable(initialValue), currentDeferred;
        plainObservable.inProgress = ko.observable(false);

        ko.computed(function () {
            if (currentDeferred) {
                currentDeferred.reject();
                currentDeferred = null;
            }

            var newDeferred = computedDeferred();
            if (newDeferred &&
                (typeof newDeferred.done == "function")) {

                // It's a deferred
                plainObservable.inProgress(true);

                // Create our own wrapper so we can reject
                currentDeferred = $.Deferred().done(function (data) {
                    plainObservable.inProgress(false);
                    plainObservable(data);
                });
                newDeferred.done(currentDeferred.resolve);
            } else {
                // A real value, so just publish it immediately
                plainObservable(newDeferred);
            }
        });

        return plainObservable;
    };

    // An extender that loads/saves observable values to local storage in the browser
    // (taken from: https://github.com/spoike/knockout.persist)
    ko.extenders.persist = function (target, key) {

        var initialValue = target();

        // Load existing value from localStorage if set
        if (key && localStorage.getItem(key) !== null) {
            try {
                initialValue = JSON.parse(localStorage.getItem(key));
            } catch (e) {
            }
        }
        target(initialValue);

        // Subscribe to new values and add them to localStorage
        target.subscribe(function (newValue) {
            localStorage.setItem(key, ko.toJSON(newValue));
        });
        return target;

    };
});
