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
            register: "/account/register",
            signIn: "/account/signin"
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
                    content: "Since the <code>users</code> table has the <code>PRIMARY KEY</code> defined as the <code>userid</code> column, Cassandra allows us to look users " +
                        "up by unique id like this:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT * FROM users\r\n" +
                        "WHERE userid = ?;" +
                        "</code></pre>" +
                        "Pretty straightforward, right?"
                },
                {
                    page: pages.userProfile,
                    target: "#register",
                    waitForTargetOn: "#navbar-main",
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
                // User registration page (unauthenticated)
                {
                    page: pages.register,
                    target: "#register-account form",
                    placement: "right",
                    content: "This is the user registration form for KillrVideo. It probably looks like many of the forms you've filled out before to register for a web " +
                        "site. Here we collect basic information like your name and email address.",
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
                },
                {
                    page: pages.register,
                    target: "#sign-in",
                    placement: "bottom", // TODO: Alignment with button
                    content: "You might have noticed that our <code>users</code> table doesn't have a <code>password</code> column and so the <code>INSERT</code> " +
                        "statement we just showed you isn't capturing that value from the form. Why not? Let's take a look at the Sign In page for an explanation. " +
                        "Click the Sign In button to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on the login link and advance the tour
                        $eventEl.one("click.guidedtour", "#sign-in", function () {
                            hs.nextStep();
                        });
                    }
                },
                // Sign In page (unauthenticated)
                {
                    page: pages.signIn,
                    target: "#signin-account",
                    placement: "right",
                    content: "This is the sign in form for KillrVideo. Once a user enters their credentials, we'll need to look them up by email address and verify " +
                        "their password.",
                    onPrev: function () {
                        // Go back to the previous page
                        window.history.back();
                    }
                },
                {
                    page: pages.signIn,
                    target: "#signin-email",
                    placement: "right",
                    content: "If our <code>users</code> table had a <code>password</code> column in it, you might be tempted to try a query like this to look a user up by email " +
                        "address:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT password FROM users\r\n" +
                        "WHERE email = ?;" +
                        "</code></pre>" +
                        "But if we try to execute that query, Cassandra will give us an <code>InvalidQuery</code> error. Why is that?"
                },
                {
                    page: pages.signIn,
                    target: "#signin-account",
                    placement: "right",
                    content: "In Cassandra, the <code>WHERE</code> clause of your query is limited to columns that are part of the <code>PRIMARY KEY</code> of the table. You'll " +
                        "remember that in our <code>users</code> table, this was the <code>userid</code> column (so that we could look users up by unique id on the user profile " +
                        "page). So how do we do a query to look a user up by email address so we can log them in?"
                },
                {
                    page: pages.signIn,
                    target: "#signin-email",
                    placement: "right",
                    content: "To solve this problem we'll create a second table in Cassandra for storing user data and make sure that it has the appropriate <code>PRIMARY KEY</code> " +
                        "definition for querying users by email address. In KillrVideo, that table looks like this:<br/><br/>" +
                        "<pre><code>" +
                        "CREATE TABLE user_credentials (\r\n" +
                        "  email text,\r\n" +
                        "  password text,\r\n" +
                        "  userid uuid\r\n" +
                        "  PRIMARY KEY (email)\r\n" +
                        ");</code></pre>"
                },
                {
                    page: pages.signIn,
                    target: "#signin-account",
                    placement: "right",
                    content: "When a user registers for the site, we'll insert the data captured into both the <code>users</code> and <code>user_credentials</code> tables. This is a " +
                        "data modeling technique called <strong>denormalization</strong> and is something that you'll use a lot when working with Cassandra."
                },
                {
                    page: pages.signIn,
                    target: "#signin-account button.btn-primary",
                    placement: "right",
                    content: "Now that we have a <code>user_credentials</code> table, we can do a query like this to look a user up by email address and verify their password:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT password FROM user_credentials\r\n" +
                        "WHERE email = ?;" +
                        "</code></pre>" +
                        "Let's sign into KillrVideo. We've filled in the form with some sample user credentials. Click the Sign In button to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Fill in some sample user credentials
                        $("#signin-email").val("guidedtour@killrvideo.com").change();
                        $("#signin-password").val("guidedtour").change();

                        // Listen for clicks on the sign in button and advance the tour
                        $eventEl.one("click.guidedtour", "#signin-account button.btn-primary", function () {
                            hs.nextStep();
                        });
                    },
                    onPrev: function() {
                        // Clear the credentials if we go back a step
                        $("#signin-email").val("").change();
                        $("#signin-password").val("").change();
                    }
                },
                // Home page (authenticated)
                {
                    page: pages.home,
                    target: "#body-content",
                    placement: "bottom",
                    content: "Yay! Back home!",
                    onPrev: function () {
                        // Log the user out, then go to the previous page
                        $.getJSON("/account/signout").then(function() {
                            window.history.back();
                        });
                    }
                },
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
                    if (currentStep.waitForTargetOn && $(currentStep.target).length === 0) {
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