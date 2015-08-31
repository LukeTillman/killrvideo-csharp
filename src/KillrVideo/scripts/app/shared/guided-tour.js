define(["knockout", "jquery", "hopscotch", "bootstrap", "arrive", "lib/knockout-extenders"], function (ko, $, hs) {

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
            signIn: "/account/signin",
            addVideo: "/videos/add",
            searchResults: new RegExp("\/search\/results")
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
                    target: "#recent-videos-header > span",
                    placement: "right",
                    content: "Now that you know a little bit more about querying and data modeling with Cassandra, let's talk about this Recent Videos section. If you remember our " +
                        "<code>videos</code> table when we were discussing the video catalog, you'll recall that it had a <code>PRIMARY KEY</code> of <code>videoid</code> for " +
                        "looking videos up by unique identifier. As you can probably guess, that table isn't going to help us for this section where we need to show the latest " +
                        "videos added to the site.",
                    onPrev: function () {
                        // Log the user out, then go to the previous page
                        $.getJSON("/account/signout").then(function() {
                            window.history.back();
                        });
                    }
                },
                {
                    page: pages.home,
                    target: "#recent-videos-header > span",
                    placement: "right",
                    content: "But by leveraging denormalization again, we can create a table that allows us to query the video data added to the site by time. In KillrVideo, the table " +
                        "looks like this:<br/><br/>" +
                        "<pre><code>" +
                        "CREATE TABLE latest_videos (\r\n" +
                        "  yyyymmdd text,\r\n" +
                        "  added_date timestamp,\r\n" +
                        "  videoid uuid,\r\n" +
                        "  userid uuid,\r\n" +
                        "  name text,\r\n" +
                        "  preview_image_location text,\r\n" +
                        "  PRIMARY KEY (yyyymmdd, added_date, videoid)\r\n" +
                        ") WITH CLUSTERING ORDER BY (\r\n" +
                        "  added_date DESC, videoid ASC);" +
                        "</code></pre>"
                },
                {
                    page: pages.home,
                    target: "#recent-videos-header > span",
                    placement: "right",
                    content: "This is a really simple example of a <strong>time series data model</strong>. Cassandra is great at storing time series data and lots of companies " +
                        "use DataStax Enterprise for use cases like the Internet of Things (IoT) which are often collecting a lot of time series data from a multitude of " +
                        "sensors or devices."
                },
                {
                    page: pages.home,
                    target: "#recent-videos-list",
                    placement: "bottom",
                    content: "One interesting thing about the <code>latest_videos</code> table is how we go about inserting data into it. In KillrVideo, we decided that the Recent " +
                        "Videos list should only ever show videos from <em>at most</em> the last 7 days. As a result, we don't really need to retain any data that's older than " +
                        "7 days. While we could write some kind of background job to delete old data from that table on a regular basis, instead we're leveraging Cassandra's " +
                        "ability to specify a <strong>TTL</strong> or <strong>Time to Live</strong> when inserting data to that table."
                },
                {
                    page: pages.home,
                    target: "#recent-videos-list",
                    placement: "bottom",
                    content: "Here's what an <code>INSERT</code> statement into the <code>latest_videos</code> table looks like:<br/><br/>" +
                        "<pre><code>" +
                        "INSERT INTO latest_videos (\r\n" +
                        "  yyyymmdd, added_date, videoid,\r\n" +
                        "  userid, name, preview_image_location)\r\n" +
                        "VALUES (?, ?, ?, ?, ?, ?)\r\n" +
                        "USING TTL 604800;" +
                        "</code></pre>" +
                        "By specifying <code>USING TTL 604800</code>, we are telling Cassandra to automatically expire or delete that record after 604,800 seconds (or 7 days)."
                },
                // TODO: Add video page next?
                {
                    page: pages.home,
                    target: "#recent-videos-list ul.list-unstyled li:first-child div.video-preview",
                    waitForTargetOn: "#recent-videos-list",
                    placement: "bottom",
                    content: "Let's look at a few other things we can do now that we're signed in to the site. Click on a video to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on any of the video previews and advance the tour
                        $eventEl.one("click.guidedtour", "#recent-videos-list div.video-preview", function () {
                            hs.nextStep();
                        });
                    }
                },
                // View video page (authenticated)
                {
                    page: pages.viewVideo,
                    target: "div.video-rating-and-sharing",
                    placement: "bottom",
                    content: "Since we're signed in, we can now rate videos as we watch them on the site. The overall rating for a video is calculated using the values " +
                        "from two <code>counter</code> columns stored in Cassandra. Here's what the table looks like:<br/><br/>" +
                        "<pre><code>" +
                        "CREATE TABLE video_ratings (\r\n" +
                        "  videoid uuid,\r\n" +
                        "  rating_counter counter,\r\n" +
                        "  rating_total counter,\r\n" +
                        "  PRIMARY KEY (videoid)\r\n" +
                        ");</code></pre>" +
                        "Columns of type <code>counter</code> are a special Cassandra column type that allow operations like increment/decrement and are great for storing " +
                        "approximate counts.",
                    onPrev: function () {
                        window.history.back();
                    }
                },
                {
                    page: pages.viewVideo,
                    target: "#view-video-comments",
                    placement: "left",
                    content: "The latest comments for a video are also displayed and now that we're signed in, we can leave comments of our own. Comments are another simple " +
                        "example of a <strong>time series data model</strong>."
                },
                {
                    page: pages.viewVideo,
                    target: "#view-video-tags",
                    placement: "bottom",
                    content: "When users add videos to the catalog, we ask them to provide tags for the video they are adding. These are just keywords that apply to the content " +
                        "of the video. Clicking on a tag is the same as using the search box in the header to search for videos with that keyword. Let's see how search works. " +
                        "Click on a tag to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on any of the video tags and advance the tour
                        $eventEl.one("click.guidedtour", "#view-video-tags a", function () {
                            hs.nextStep();
                        });
                    }
                },
                // Search results page (authenticated)
                {
                    page: pages.searchResults,
                    target: "#body-content h3.section-divider > span",
                    placement: "right",
                    content: "Here we see can see the search results for the keyword we clicked on. Searching for videos on KillrVideo is powered by the Search feature of " +
                        "DataStax Enterprise. This feature creates indexes that allow us to do powerful Lucene queries on our video catalog data that are not possible to do " +
                        "with just CQL. The indexes are automatically updated in the background as new data is added to our catalog tables in Cassandra.",
                    onPrev: function () {
                        window.history.back();
                    }
                },
                {
                    page: pages.searchResults,
                    target: "#body-content h3.section-divider > span",
                    placement: "right",
                    content: "On KillrVideo, we've enabled DataStax Enterprise Search on our <code>videos</code> table which holds our video catalog. When a user searches for " +
                        "videos, we're then able to issue a Lucene query that searches for relevant videos. For example, we could use a Lucene query " +
                        "like this to search for videos with the word <em>cassandra</em> in the <code>description</code> column:<br/><br/>" +
                        "<pre><code>" +
                        "description:cassandra" +
                        "</code></pre>"
                },
                {
                    page: pages.searchResults,
                    target: "#body-content h3.section-divider > span",
                    placement: "right",
                    content: "Lucene queries in DataStax Enterprise are also integrated with CQL, so I can get data from the <code>videos</code> table using that query like this:<br/><br/>" +
                        "<pre><code>" +
                        "SELECT * FROM videos\r\n" +
                        "WHERE solr_query = 'description:cassandra';" +
                        "</code></pre>"
                },
                {
                    page: pages.searchResults,
                    target: "#body-content h3.section-divider > span",
                    placement: "right",
                    content: "But we're not limited to just querying on a single field. Since DataStax Enterprise Search is powered by Solr, we have all that power available " +
                        "to us as well. The query we issue to return search results on this page for <em>cassandra</em> actually looks like this:<br/><br/>" +
                        "<pre><code>" +
                        "{ 'q': '{!edismax qf=\"name^2 tags^1 description\"}cassandra' }" +
                        "</code></pre>" +
                        "This uses Solr's ExtendedDisMax parser to search across multiple columns and gives a boost to search results with <em>cassandra</em> in the <code>name</code> " +
                        "or <code>tags</code> columns over just the <code>description</code> column."
                },
                {
                    page: pages.searchResults,
                    target: "#search-results div.video-preview:first-child",
                    placement: "bottom",
                    content: "There are some other cool things we can do with DataStax Enterprise Search beyond just full-text searching. Let's look at a video again to check out " +
                        "another KillrVideo feature powered by DSE Search. Click on a video to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on any of the videos and advance the tour
                        $eventEl.one("click.guidedtour", "div.video-preview", function () {
                            hs.nextStep();
                        });
                    }
                },
                // View video page (authenticated)
                {
                    page: pages.viewVideo,
                    target: "#view-video-related",
                    placement: "top",
                    content: "Down here we see a list of videos that are related to the one we're currently viewing. This list is also powered by DataStax Enterprise Search. By " +
                        "turning on Solr's MoreLikeThis feature in DSE, we can issue queries that will return videos with similar terms (in their titles, descriptions, etc.) to the " +
                        "video we're currently viewing.",
                    onPrev: function () {
                        window.history.back();
                    }
                },
                {
                    page: pages.viewVideo,
                    target: "#logo",
                    placement: "bottom",
                    content: "DataStax Enterprise also offers some other interesting features beyond just Search. Let's go back to the home page to take a look at another one of those. " +
                        "Click on the KillrVideo logo to continue.",
                    showNextButton: false,
                    multipage: true,
                    onShow: function () {
                        // Listen for clicks on any of the videos and advance the tour
                        $eventEl.one("click.guidedtour", "#logo", function () {
                            hs.nextStep();
                        });
                    }
                },
                // Home page (authenticated)
                {
                    page: pages.home,
                    target: "#body-content",
                    placement: "left",
                    content: "TODO: Video recommendations with Spark.",
                    onPrev: function () {
                        window.history.back();
                    }
                },
                {
                    page: pages.home,
                    target: "#logo",
                    placement: "bottom",
                    content: "Thanks for taking the time to learn more about KillrVideo! Remember, KillrVideo is completely open source, so check it out on GitHub " +
                        "to dig deeper into how things work. There's also great self-paced training courses for DataStax Enterprise available online at the DataStax " +
                        "Academy web site. If you're new to Cassandra, be sure to check that out as well."
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