define(["jquery", "arrive"], function ($) {
    // A resolved promise
    var resolvedPromise = $.Deferred().resolve().promise();

    // A promise that will resolve when the DOM is ready
    var domReadyPromise = $.Deferred(function(defer) {
        $(function() {
            defer.resolve();
        });
    }).promise();

    // Returns a promise that will wait for an element to arrive in the DOM if it's not currently present
    function waitForElementIfNotPresent(selector, ancestor) {
        return domReadyPromise.then(function() {
            if ($(selector).length > 0) {
                return resolvedPromise;
            }

            var defer = $.Deferred();
            $(ancestor).arrive(selector, { onceOnly: true }, function() {
                defer.resolve();
            });
            return defer.promise();
        });
    };

    function addNextOnClickHandler(selector, tour) {
        $("body").one("click.tour", selector, function() {
            tour.next();
        });
    }

    function removeNextOnClickHandler() {
        $("body").off("click.tour");
    }
    
    // Return the tour definition
    return {
        // Unique Id for the tour
        tourId: "KillrVideo",

        // The various pages on the site either as a path or RegExp, used by the steps to test if a user
        // is on the correct page
        pages: {
            home: { url: "/", authenticated: false },
            homeAuthenticated: { url: "/", authenticated: true },
            viewVideo: { url: new RegExp("\/view\/"), authenticated: false },
            viewVideoAuthenticated: { url: new RegExp("\/view\/"), authenticated: true },
            userProfile: { url: new RegExp("\/account\/info"), authenticated: false },
            register: { url: "/account/register", authenticated: false },
            signIn: { url: "/account/signin", authenticated: false },
            searchResultsAuthenticated: { url: new RegExp("\/search\/results"), authenticated: true }
        },

        // The tour step definitions
        steps: [
            // Starting on the Home Page (Unauthenticated)
            {
                page: "home",
                target: "#logo",
                placement: "bottom",
                title: "Welcome to KillrVideo!",
                content: "KillrVideo is an open source demo video sharing application built on DataStax Enterprise powered by " +
                    "Apache Cassandra. If you're new to Cassandra, this demo is a great way to learn more about how to build your own " +
                    "applications and services on top of DataStax Enterprise.",
                showPreviousButton: false
            },
            {
                page: "home",
                target: "#logo",
                placement: "bottom",
                content: "This tour will walk you through some of the features of the site. Along the way we'll point out some of the " +
                    "interesting technical things going on behind the scenes and provide you with links for learning more about those " +
                    "topics.",
                links: [
                    { label: "KillrVideo on GitHub", url: "https://github.com/luketillman/killrvideo-csharp" },
                    { label: "DataStax Enterprise", url: "http://www.datastax.com/products/datastax-enterprise" }
                ]
            },
            {
                page: "home",
                target: "#tour-enabled",
                placement: "bottom",
                content: "You can toggle this guided tour off at any time using this button. Turning it back on will take you to the home page and " +
                    "restart the tour from the beginning."
            },
            {
                page: "home",
                target: "#recent-videos-list ul.list-unstyled li:first-child div.video-preview",
                placement: "bottom",
                content: "Let's get started by looking at a video.",
                callToAction: "Click on a video to continue.",
                beforeShowPromise: function () { return waitForElementIfNotPresent(this.target, "#recent-videos-list"); },
                showNextButton: false,
                onShow: function(tour) { addNextOnClickHandler("#recent-videos-list div.video-preview", tour); },
                onHide: function() { removeNextOnClickHandler(); }
            },
            // View video page (unauthenticated)
            {
                page: "viewVideo",
                target: "#logo",    // TODO: Make window?
                placement: "bottom",
                content: "This is the View Video page where users can playback videos added to the site. Details like the video's description and name " +
                    "are stored in a catalog in Cassandra, similar to how a Product Catalog might work on an e-commerce site. Cassandra Query Language or " +
                    "CQL makes it easy to store this information. If you're experienced with SQL syntax from working with relational databases, it will " +
                    "look very familiar to you.",
                links: [
                    { label: "Product Catalogs using Cassandra", url: "http://www.planetcassandra.org/blog/functional_use_cases/product-catalogs-playlists/" }
                ]
            },
            {
                page: "viewVideo",
                target: "#logo",    // TODO: Make window?
                placement: "bottom",
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
                    ");</code></pre>",
                contentClass: "wide",
                links: [
                    { label: "Creating a Table with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useCreateTableTOC.html" },
                    { label: "CREATE TABLE reference", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_reference/create_table_r.html" }
                ]
            },
            {
                page: "viewVideo",
                target: "#view-video-title",
                placement: "bottom",
                content: "Querying the data from that <code>videos</code> table to show things like the video title is also easy with CQL. Here's what the query looks like " +
                    "to retrieve a video from the catalog in Cassandra:<br/><br/>" +
                    "<pre><code>" +
                    "SELECT * FROM videos\r\n" +
                    "WHERE videoid = ?;" +
                    "</code></pre>" +
                    "In CQL, the <code>?</code> character is used as a placeholder for bind variable values. If you've ever done parameterized SQL queries before, the idea " +
                    "is the same.",
                links: [
                    { label: "Querying Tables with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useQueryDataTOC.html" },
                    { label: "SELECT statement reference", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_reference/select_r.html" }
                ]
            },
            {
                page: "viewVideo",
                target: "#view-video-author",
                placement: "bottom",
                content: "Videos are added to the catalog by users on the site. Let's take a look at this user's profile.",
                callToAction: "Click on the author for this video to continue.",
                showNextButton: false,
                onShow: function(tour) { addNextOnClickHandler("#view-video-author > a", tour); },
                onHide: function() { removeNextOnClickHandler(); }
            },
            // View user profile (unauthenicated)
            {
                page: "userProfile",
                target: "#logo",    // TODO: Make window?
                placement: "bottom",
                content: "This is the user profile page. Here you can see basic information about a user, along with any comments they've made on the site and " +
                    "any videos they've added to the catalog."
            },
            {
                page: "userProfile",
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
                page: "userProfile",
                target: "#user-profile-header",
                placement: "right",
                content: "Since the <code>users</code> table has the <code>PRIMARY KEY</code> defined as the <code>userid</code> column, Cassandra allows us to look users " +
                    "up by unique id like this:<br/><br/>" +
                    "<pre><code>" +
                    "SELECT * FROM users\r\n" +
                    "WHERE userid = ?;" +
                    "</code></pre>" +
                    "Pretty straightforward, right?",
                links: [
                    { label: "Simple Primary Keys with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useSimplePrimaryKeyConcept.html" }
                ]
            },
            {
                page: "userProfile",
                target: "#register",
                waitForTargetOn: "#navbar-main",
                placement: "bottom",
                content: "Most of the interesting things you can do on KillrVideo like adding videos to the catalog or leaving comments on a video, require you to " +
                    "be logged in as a registered user. Let's take a look at the user registration process.",
                callToAction: "Click on the Register button to continue.",
                showNextButton: false,
                onShow: function(tour) { addNextOnClickHandler("#register", tour); },
                onHide: function() { removeNextOnClickHandler(); }
            },
            // User registration page (unauthenticated)
            {
                page: "register",
                target: "#register-account form",
                placement: "right",
                content: "This is the user registration form for KillrVideo. It probably looks like many of the forms you've filled out before to register for a web " +
                    "site. Here we collect basic information like your name and email address."
            },
            {
                page: "register",
                target: "#register-account form button.btn-primary",
                placement: "top",
                content: "When a user submits the form, we insert the data into Cassandra. Here's what it looks like to use a CQL <code>INSERT</code> " +
                    "statement to add data to our <code>users</code> table:<br/><br/>" +
                    "<pre><code>" +
                    "INSERT INTO users (\r\n" +
                    "  userid, firstname, lastname,\r\n" +
                    "  email, created_date)\r\n" +
                    "VALUES (?, ?, ?, ?, ?);" +
                    "</code></pre>",
                contentClass: "wide",
                links: [
                    { label: "Inserting and Updating Data with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useInsertDataTOC.html" },
                    { label: "INSERT statement reference", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_reference/insert_r.html" }
                ]
            },
            {
                page: "register",
                target: "#sign-in",
                placement: "bottom",
                content: "You might have noticed that our <code>users</code> table doesn't have a <code>password</code> column and so the <code>INSERT</code> " +
                    "statement we just showed you isn't capturing that value from the form. Why not? Let's take a look at the Sign In page for an explanation.",
                callToAction: "Click the Sign In button to continue.",
                showNextButton: false,
                onShow: function(tour) { addNextOnClickHandler("#sign-in", tour); },
                onHide: function() { removeNextOnClickHandler(); }
            },
            // Sign In page (unauthenticated)
            {
                page: "signIn",
                target: "#signin-account",
                placement: "right",
                content: "This is the sign in form for KillrVideo. Once a user enters their credentials, we'll need to look them up by email address and verify " +
                    "their password."
            },
            {
                page: "signIn",
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
                page: "signIn",
                target: "#signin-account",
                placement: "right",
                content: "In Cassandra, the <code>WHERE</code> clause of your query is limited to columns that are part of the <code>PRIMARY KEY</code> of the table. You'll " +
                    "remember that in our <code>users</code> table, this was the <code>userid</code> column (so that we could look users up by unique id on the user profile " +
                    "page). So how do we do a query to look a user up by email address so we can log them in?",
                links: [
                    { label: "Filtering Data using WHERE in CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_reference/select_r.html?scroll=reference_ds_d35_v2q_xj__filtering-data-using-where" }
                ]
            },
            {
                page: "signIn",
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
                    ");</code></pre>",
                contentClass: "wide"
            },
            {
                page: "signIn",
                target: "#signin-account",
                placement: "right",
                content: "When a user registers for the site, we'll insert the data captured into both the <code>users</code> and <code>user_credentials</code> tables. This is a " +
                    "data modeling technique called <strong>denormalization</strong> and is something that you'll use a lot when working with Cassandra.",
                links: [
                    { label: "DS220: Data Modeling Course", url: "https://academy.datastax.com/courses/ds220-data-modeling" },
                    { label: "Data Modeling with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/ddl/ddlCQLDataModelingTOC.html" }
                ]
            },
            {
                page: "signIn",
                target: "#signin-account button.btn-primary",
                placement: "right",
                content: "Now that we have a <code>user_credentials</code> table, we can do a query like this to look a user up by email address and verify their password:<br/><br/>" +
                    "<pre><code>" +
                    "SELECT password\r\n" +
                    "FROM user_credentials\r\n" +
                    "WHERE email = ?;" +
                    "</code></pre>" +
                    "Let's sign into KillrVideo. We've filled in the form with some sample user credentials.",
                callToAction: "Click the Sign In button to continue.",
                showNextButton: false,
                onShow: function (tour) {
                    // Fill in some sample user credentials
                    $("#signin-email").val("guidedtour@killrvideo.com").change();
                    $("#signin-password").val("guidedtour").change();

                    addNextOnClickHandler("#signin-account button.btn-primary", tour);  // TODO: Next on valid authentication, not just click
                }, 
                onHide: function() {
                    removeNextOnClickHandler();
                }
            },
            // Home page (authenticated)
            {
                page: "homeAuthenticated",
                target: "#recent-videos-header > span",
                placement: "right",
                content: "Now that you know a little bit more about querying and data modeling with Cassandra, let's talk about this Recent Videos section. If you remember our " +
                    "<code>videos</code> table when we were discussing the video catalog, you'll recall that it had a <code>PRIMARY KEY</code> of <code>videoid</code> for " +
                    "looking videos up by unique identifier. As you can probably guess, that table isn't going to help us for this section where we need to show the latest " +
                    "videos added to the site."
            },
            {
                page: "homeAuthenticated",
                target: "#recent-videos-header > span",
                placement: "right",
                content: "But by leveraging denormalization again, we can create a table that allows us to query the video data added to the site by time. In KillrVideo, the" +
                    "<code>latest_videos</code> table looks like this:<br/><br/>" +
                    "<pre><code>" +
                    "CREATE TABLE latest_videos (\r\n" +
                    "  yyyymmdd text,\r\n" +
                    "  added_date timestamp,\r\n" +
                    "  videoid uuid,\r\n" +
                    "  userid uuid,\r\n" +
                    "  name text,\r\n" +
                    "  preview_image_location text,\r\n" +
                    "  PRIMARY KEY (\r\n" +
                    "    yyyymmdd, added_date, videoid)\r\n" +
                    ") WITH CLUSTERING ORDER BY (\r\n" +
                    "  added_date DESC, videoid ASC);" +
                    "</code></pre>",
                contentClass: "wide",
                links: [
                    { label: "Compound Primary Keys with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useCompoundPrimaryKeyConcept.html" }
                ]
            },
            {
                page: "homeAuthenticated",
                target: "#recent-videos-header > span",
                placement: "right",
                content: "This is a really simple example of a <strong>time series data model</strong>. Cassandra is great at storing time series data and lots of companies " +
                    "use DataStax Enterprise for use cases like the Internet of Things (IoT) which are often collecting a lot of time series data from a multitude of " +
                    "sensors or devices.",
                links: [
                    { label: "Cassandra for IoT and Sensor Data", url: "http://www.planetcassandra.org/blog/functional_use_cases/internet-of-things-sensor-data/" }
                ]
            },
            {
                page: "homeAuthenticated",
                target: "#recent-videos-list > div",
                placement: "bottom",
                content: "One interesting thing about the <code>latest_videos</code> table is how we go about inserting data into it. In KillrVideo, we decided that the Recent " +
                    "Videos list should only ever show videos from <em>at most</em> the last 7 days. As a result, we don't really need to retain any data that's older than " +
                    "7 days. While we could write some kind of background job to delete old data from that table on a regular basis, instead we're leveraging Cassandra's " +
                    "ability to specify a <strong>TTL</strong> or <strong>Time to Live</strong> when inserting data to that table.",
                contentClass: "wide",
                beforeShowPromise: function () { return waitForElementIfNotPresent("#recent-videos-list ul.list-unstyled li:first-child div.video-preview", "#recent-videos-list"); }
            },
            {
                page: "homeAuthenticated",
                target: "#recent-videos-list > div",
                placement: "bottom",
                content: "Here's what an <code>INSERT</code> statement into the <code>latest_videos</code> table looks like:<br/><br/>" +
                    "<pre><code>" +
                    "INSERT INTO latest_videos (\r\n" +
                    "  yyyymmdd, added_date, videoid,\r\n" +
                    "  userid, name,\r\n" +
                    "  preview_image_location)\r\n" +
                    "VALUES (?, ?, ?, ?, ?, ?)\r\n" +
                    "USING TTL 604800;" +
                    "</code></pre>" +
                    "By specifying <code>USING TTL 604800</code>, we are telling Cassandra to automatically expire or delete that record after 604,800 seconds (or 7 days).",
                contentClass: "wide",
                links: [
                    { label: "Expiring Data with Time-To-Live", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useExpire.html" }
                ],
                beforeShowPromise: function () { return waitForElementIfNotPresent("#recent-videos-list ul.list-unstyled li:first-child div.video-preview", "#recent-videos-list"); }
            },
            // TODO: Add video page next?
            {
                page: "homeAuthenticated",
                target: "#recent-videos-list ul.list-unstyled li:first-child div.video-preview",
                waitForTargetOn: "#recent-videos-list",
                placement: "bottom",
                content: "Let's look at a few other things we can do now that we're signed in to the site.",
                callToAction: "Click on a video to continue.",
                beforeShowPromise: function() { return waitForElementIfNotPresent(this.target, "#recent-videos-list"); },
                showNextButton: false,
                onShow: function (tour) { addNextOnClickHandler("#recent-videos-list div.video-preview", tour); }, 
                onHide: function() { removeNextOnClickHandler(); }
            },
            // View video page (authenticated)
            {
                page: "viewVideoAuthenticated",
                target: "div.video-rating-and-sharing",
                placement: "bottom",
                content: "Since we're signed in, we can now rate videos as we watch them on the site. The overall rating for a video is calculated using the values " +
                    "from two <code>counter</code> columns stored in Cassandra."
            },
            {
                page: "viewVideoAuthenticated",
                target: "div.video-rating-and-sharing",
                placement: "bottom",
                content: "Here's what the <code>video_ratings</code> table looks like:<br/><br/>" +
                    "<pre><code>" +
                    "CREATE TABLE video_ratings (\r\n" +
                    "  videoid uuid,\r\n" +
                    "  rating_counter counter,\r\n" +
                    "  rating_total counter,\r\n" +
                    "  PRIMARY KEY (videoid)\r\n" +
                    ");</code></pre>" +
                    "Columns of type <code>counter</code> are a special Cassandra column type that allow operations like increment/decrement and are great for storing " +
                    "approximate counts.",
                contentClass: "wide",
                links: [
                    { label: "Creating a Counter table with CQL", url: "http://docs.datastax.com/en/cql/3.3/cql/cql_using/useCountersConcept.html" }
                ]
            },
            {
                page: "viewVideoAuthenticated",
                target: "#view-video-comments > h5",
                placement: "left",
                content: "The latest comments for a video are also displayed and now that we're signed in, we can leave comments of our own. Comments are another simple " +
                    "example of a <strong>time series data model</strong>."
            },
            {
                page: "viewVideoAuthenticated",
                target: "#view-video-tags a:first-child",
                placement: "bottom",
                content: "When users add videos to the catalog, we ask them to provide tags for the video they are adding. These are just keywords that apply to the content " +
                    "of the video. Clicking on a tag is the same as using the search box in the header to search for videos with that keyword. Let's see how search works.",
                callToAction: "Click on a tag to continue.",
                showNextButton: false,
                onShow: function (tour) { addNextOnClickHandler("#view-video-tags a", tour); }, 
                onHide: function() { removeNextOnClickHandler(); }
            },
            // Search results page (authenticated)
            {
                page: "searchResultsAuthenticated",
                target: "#body-content h3.section-divider > span",
                placement: "right",
                content: "Here we see can see the search results for the keyword we clicked on. Searching for videos on KillrVideo is powered by the Search feature of " +
                    "DataStax Enterprise. This feature creates indexes that allow us to do powerful Lucene queries on our video catalog data that are not possible to do " +
                    "with just CQL. The indexes are automatically updated in the background as new data is added to our catalog tables in Cassandra.",
                links: [
                    { label: "DataStax Enterprise Search", url: "http://www.datastax.com/products/datastax-enterprise-search" }
                ]
            },
            {
                page: "searchResultsAuthenticated",
                target: "#body-content h3.section-divider > span",
                placement: "right",
                content: "On KillrVideo, we've enabled DataStax Enterprise Search on our <code>videos</code> table which holds our video catalog. When a user searches for " +
                    "videos, we're then able to issue a Lucene query that searches for relevant videos. For example, we could use a Lucene query " +
                    "like this to search for videos with the word <em>cassandra</em> in the <code>description</code> column:<br/><br/>" +
                    "<pre><code>" +
                    "description:cassandra" +
                    "</code></pre>",
                links: [
                    { label: "Queries in DSE Search", url: "http://docs.datastax.com/en/datastax_enterprise/4.7/datastax_enterprise/srch/srchQry.html" }
                ]
            },
            {
                page: "searchResultsAuthenticated",
                target: "#body-content h3.section-divider > span",
                placement: "right",
                content: "Lucene queries in DataStax Enterprise are also integrated with CQL, so I can get data from the <code>videos</code> table using that query like this:<br/><br/>" +
                    "<pre><code>" +
                    "SELECT * FROM videos\r\n" +
                    "WHERE solr_query =\r\n" +
                    "  'description:cassandra';" +
                    "</code></pre>",
                links: [
                    { label: "CQL Solr Queries in DSE Search", url: "http://docs.datastax.com/en/datastax_enterprise/4.7/datastax_enterprise/srch/srchCql.html" }
                ]
            },
            {
                page: "searchResultsAuthenticated",
                target: "#body-content h3.section-divider > span",
                placement: "right",
                content: "But we're not limited to just querying on a single field. Since DataStax Enterprise Search is powered by Solr, we have all that power available " +
                    "to us as well. The query we issue to return search results on this page for <em>cassandra</em> actually looks like this:<br/><br/>" +
                    "<pre><code>" +
                    "{ 'q': '{!edismax qf=\"name^2 tags^1 description\"}cassandra' }" +
                    "</code></pre>" +
                    "This uses Solr's Extended DisMax parser to search across multiple columns and gives a boost to search results with <em>cassandra</em> in the <code>name</code> " +
                    "or <code>tags</code> columns over just the <code>description</code> column.",
                contentClass: "wide",
                links: [
                    { label: "JSON Queries in DSE Search", url: "http://docs.datastax.com/en/datastax_enterprise/4.7/datastax_enterprise/srch/srchJSON.html" },
                    { label: "Extended DisMax Parser", url: "https://cwiki.apache.org/confluence/display/solr/The+Extended+DisMax+Query+Parser" },
                ]
            },
            {
                page: "searchResultsAuthenticated",
                target: "#search-results div.row div.col-sm-3:first-child div.video-preview",
                placement: "bottom",
                content: "There are some other cool things we can do with DataStax Enterprise Search beyond just full-text searching. Let's look at a video again to check out " +
                    "another KillrVideo feature powered by DSE Search.",
                callToAction: "Click on a video to continue.",
                showNextButton: false,
                onShow: function (tour) { addNextOnClickHandler("div.video-preview", tour); }, 
                onHide: function() { removeNextOnClickHandler(); }
            },
            // View video page (authenticated)
            {
                page: "viewVideoAuthenticated",
                target: "#view-video-related div.video-preview-list",
                placement: "top",
                content: "Down here we see a list of videos that are related to the one we're currently viewing. This list is also powered by DataStax Enterprise Search. By " +
                    "turning on Solr's MoreLikeThis feature in DSE, we can issue queries that will return videos with similar terms (in their titles, descriptions, etc.) to the " +
                    "video we're currently viewing.",
                beforeShowPromise: function () { return waitForElementIfNotPresent(this.target, "#view-video-related"); },
                links: [
                    { label: "MoreLikeThis Component", url: "https://cwiki.apache.org/confluence/display/solr/MoreLikeThis" }
                ]
            },
            {
                page: "viewVideoAuthenticated",
                target: "#logo",
                placement: "bottom",
                content: "DataStax Enterprise also offers some other interesting features beyond just Search. Let's go back to the home page to take a look at another one of those.",
                callToAction: "Click on the KillrVideo logo to continue.",
                showNextButton: false,
                onShow: function (tour) { addNextOnClickHandler("#logo", tour); }, 
                onHide: function() { removeNextOnClickHandler(); }
            },
            // Home page (authenticated)
            {
                page: "homeAuthenticated",
                target: "#logo",
                placement: "bottom",
                content: "Coming soon, video recommendations with DSE Analytics and Spark."
            },
            {
                page: "homeAuthenticated",
                target: "#logo",
                placement: "bottom",
                content: "Thanks for taking the time to learn more about KillrVideo! Remember, KillrVideo is completely open source, so check it out on GitHub " +
                    "to dig deeper into how things work. There's also great self-paced training courses for DataStax Enterprise available online at the DataStax " +
                    "Academy web site.",
                contentClass: "wide",
                showNextButton: false,
                links: [
                    { label: "KillrVideo on GitHub", url: "https://github.com/luketillman/killrvideo-csharp" },
                    { label: "Free Online Courses at DataStax Academy", url: "https://academy.datastax.com/" },
                    { label: "Planet Cassandra", url: "http://www.planetcassandra.org/" },
                    { label: "Companies Using DataStax Enterprise", url: "http://www.datastax.com/customers" }
                ]
            }
        ]
    };
});