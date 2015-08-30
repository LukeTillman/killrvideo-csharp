define(["knockout", "jquery", "hopscotch", "arrive", "lib/knockout-extenders"], function (ko, $, hs) {

    // Return a view mdoel for the guided tour
    return function guidedTourModel() {
        var self = this,
            $eventEl = $("body");

        // The various pages on the site either as a path or RegExp, used by the steps to test if a user
        // is on the correct page
        var pages = {
            home: "/",
            viewVideo: new RegExp("\/view\/"),
            userProfile: new RegExp("\/account\/info"),
            register: "/account/register"
        };
            
        // Create the tour
        var tour = {
            id: "KillrVideo",
            onStart: function() { self.enabled(true); },
            onEnd: function () {
                // Remove any event listeners attached by the tour and disable
                $eventEl.off(".guidedtour");
                self.enabled(false);
            },
            onClose: function() {
                // Remove any event listeners attached by the tour and disable
                $eventEl.off(".guidedtour");
                self.enabled(false);
            },
            onPrev: function() {
                // Remove any event listeners that might have been attached by the step we're navigating away from
                $eventEl.off(".guidedtour");
            },
            showPrevButton: true,
            steps: [
                // Starting on the Home Page (Unauthenticated)
                {
                    page: pages.home,
                    target: "#logo",
                    placement: "bottom",
                    title: "Welcome to KillrVideo!",
                    content: "KillrVideo is an open source demo video sharing application built on DataStax Enterprise powered by " +
                        "Apache Cassandra. If you're new to Cassandra, this demo is a great way to learn more about how to build your own " +
                        "applications and services on top of DataStax Enterprise."
                },
                {
                    page: pages.home,
                    target: "#logo",
                    placement: "bottom",
                    content: "This tour will walk you through some of the features of the site. Along the way we'll point out some of the " +
                        "interesting technical things going on behind the scenes and provide you with links for learning more about those " +
                        "topics."
                },
                {
                    page: pages.home,
                    target: "#tour-enabled",
                    placement: "bottom",
                    content: "You can toggle this guided tour off at any time using this button. Turning it back on will take you to the home page and " +
                        "restart the tour from the beginning."
                },
                {
                    page: pages.home,
                    target: "#recent-videos-list ul.list-unstyled li:first-child div.video-preview",
                    waitForTargetOn: "#recent-videos-list",
                    placement: "bottom",
                    content: "Let's get started by looking at a video. Click on a video to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function() {
                        // Listen for clicks on any of the video previews and advance the tour
                        $eventEl.one("click.guidedtour", "#recent-videos-list div.video-preview", function() {
                            hs.nextStep();
                        });
                    }
                },
                // View video page (unauthenticated)
                {
                    page: pages.viewVideo,
                    target: "#body-content",
                    xOffset: "center",
                    yOffset: "center",
                    placement: "bottom",
                    content: "This is the View Video page where users can playback videos added to the site. Details like the video's description and name " +
                        "are stored in a catalog in Cassandra, similar to how a Product Catalog might work on an e-commerce site. Cassandra Query Language or " +
                        "CQL makes it easy to store this information. If you're experienced with SQL syntax from working with relational databases, it will " +
                        "look very familiar to you.",
                    onPrev: function() {
                        // Go back to the previous page
                        window.history.back();
                    }
                },
                {
                    page: pages.viewVideo,
                    target: "#body-content",
                    xOffset: "center",
                    yOffset: "center",
                    placement: "bottom",
                    width: 350,
                    content: "Here's what the <code>videos</code> table for the catalog looks like in CQL:<br/><br/>" +
                        "<pre><code>" +
                        "CREATE TABLE videos (\r\n" +
                        "  videoid uuid,\r\n" +
                        "  userid uuid,\r\n" +
                        "  name text,\r\n" +
                        "  description text,\r\n" +
                        "  location text,\r\n" +
                        "  location_type int,\r\n" +
                        "  preview_image_location text,\r\n" +
                        "  tags set&lt;text&gt;,\r\n" +
                        "  added_date timestamp,\r\n" +
                        "  PRIMARY KEY (videoid)\r\n" +
                        ");</code></pre>"
                },
                {
                    page: pages.viewVideo,
                    target: "#view-video-title",
                    placement: "bottom",
                    content: "Querying the data from that <code>videos</code> table to show things like the video title is also easy with CQL. Here's what the query looks like " +
                        "to retrieve a video from the catalog in Cassandra:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT * FROM videos\r\n" +
                        "WHERE videoid = ?;" +
                        "</code></pre>" +
                        "In CQL, the <code>?</code> character is used as a placeholder for bind variable values. If you've ever done parameterized SQL queries before, the idea " +
                        "is the same."
                },
                {
                    page: pages.viewVideo,
                    target: "#view-video-author",
                    placement: "bottom",
                    content: "Videos are added to the catalog by users on the site. Let's take a look at this user's profile. Click on the author for this video to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function() {
                        // Listen for clicks on the author and advance the tour
                        $eventEl.one("click.guidedtour", "#view-video-author > a", function () {
                            hs.nextStep();
                        });
                    }
                },
                // View user profile (unauthenicated)
                {
                    page: pages.userProfile,
                    target: "#body-content",
                    xOffset: "center",
                    yOffset: "center",
                    placement: "bottom",
                    content: "This is the user profile page. Here you can see basic information about a user, along with any comments they've made on the site and " +
                        "any videos they've added to the catalog.",
                    onPrev: function() {
                        // Go back to the previous page
                        window.history.back();
                    }
                },
                {
                    page: pages.userProfile,
                    target: "#user-profile-header",
                    placement: "right",
                    content: "Just like the video catalog data, basic profile information for a user is stored in a CQL table and users can be looked up by unique " +
                        "id. Here's what the <code>users</code> table looks like:<br/><br/>" +
                        "<pre><code>" +
                        "CREATE TABLE users (\r\n" +
                        "  userid uuid,\r\n" +
                        "  firstname text,\r\n" +
                        "  lastname text,\r\n" +
                        "  email text,\r\n" +
                        "  created_date timestamp,\r\n" +
                        "  PRIMARY KEY (userid)\r\n" +
                        ");</code></pre>"
                },
                {
                    page: pages.userProfile,
                    target: "#user-profile-header",
                    placement: "right",
                    content: "To look a user up by id, we can query the <code>users</code> table like this:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT * FROM users\r\n" +
                        "WHERE userid = ?;" +
                        "</code></pre>" +
                        "Pretty straightforward, right?"
                },
                {
                    page: pages.userProfile,
                    target: "#register",
                    placement: "bottom",
                    xOffset: -220,
                    arrowOffset: 250,
                    content: "Most of the interesting things you can do on KillrVideo like adding videos to the catalog or leaving comments on a video, require you to " +
                        "be logged in as a registered user. Let's take a look at the user registration process. Click on the Register button to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on the register link and advance the tour
                        $eventEl.one("click.guidedtour", "#register", function () {
                            hs.nextStep();
                        });
                    }
                },
                {
                    page: pages.register,
                    target: "#register-account form",
                    placement: "right",
                    content: "This is the user registration form for KillrVideo. It probably looks a lot like the forms you've filled out before on most web sites, " +
                        "collecting basic information like your name and email address.",
                    onPrev: function () {
                        // Go back to the previous page
                        window.history.back();
                    }
                },
                {
                    page: pages.register,
                    target: "#register-account form button.btn-primary",
                    placement: "top",
                    content: "When a user submits the form, we insert the data into Cassandra. Here's what it looks like to use a CQL <code>INSERT</code> " +
                        "statement to add data to our <code>users</code> table:<br/><br/>" +
                        "<pre><code>" +
                        "INSERT INTO users (\r\n" +
                        "  userid, firstname, lastname, email, created_date)\r\n" +
                        "VALUES (?, ?, ?, ?, ?);" +
                        "</code></pre>"
                }
            ]
        };

        // Whether or not the tour is currently enabled
        self.enabled = ko.observable(true).extend({ persist: "tourEnabled" });

        // Toggles enabled on/off
        self.toggleEnabled = function () {
            var isEnabled = self.enabled();
            if (isEnabled) {
                hs.endTour();
            } else {
                // TODO: Ensure logged out
                if (window.location.pathname !== "/") {
                    self.enabled(true);
                    window.location.href = "/";
                    return;
                }

                hs.startTour(tour, 0);
            }
        };

        // Determine whether to start the tour
        var startTour = self.enabled();
        if (startTour) {
            // If starting the tour, check for the tour state and do some extra validation
            var tourState = hs.getState();
            if (tourState) {
                // Figure out what step we're on and make sure we're on the correct page and/or in the right state
                var stepIdx = parseInt(tourState.split(":")[1]);
                var currentStep = tour.steps[stepIdx];
                
                // Test that we're on the correct page (possibly using a RegExp)
                var onCorrectPage = (typeof currentStep.page === "string")
                    ? currentStep.page === window.location.pathname.toLowerCase()
                    : currentStep.page.test(window.location.pathname.toLowerCase());
                
                if (onCorrectPage) {
                    if (currentStep.waitForTargetOn) {
                        // The target element is dynamic and might not be on the page yet, so wait for it before starting the tour
                        $(currentStep.waitForTargetOn).arrive(currentStep.target, { onceOnly: true }, function () {
                            hs.startTour(tour);
                        });
                        startTour = false;
                    }
                } else {
                    // We're on the wrong page, so end the tour and don't start it
                    hs.endTour();
                    self.enabled(false);
                    startTour = false;
                }
            }
        }
        
        // Start the tour on DOM ready if necessary
        if (startTour) {
            $(function() {
                hs.startTour(tour);
            });
        }
    };
});