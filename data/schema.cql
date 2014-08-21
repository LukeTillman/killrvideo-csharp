// Drop and recreate the keyspace
DROP KEYSPACE IF EXISTS killrvideo;
CREATE KEYSPACE killrvideo WITH REPLICATION = { 'class' : 'SimpleStrategy', 'replication_factor' : 1 };

// Create the schema
USE killrvideo;

// User credentials, keyed by email address so we can authenticate
CREATE TABLE user_credentials (
    email text,
    password text,
    userid uuid,
    PRIMARY KEY (email)
);

// Users keyed by id
CREATE TABLE users (
    userid uuid,
    firstname text,
    lastname text,
    email text,
    created_date timestamp,
    PRIMARY KEY (userid)
);

// Videos by id
CREATE TABLE videos (
    videoid uuid,
    userid uuid,
    name text,
    description text,
    location text,
    location_type int,
    preview_image_location text,
    tags set<text>,
    added_date timestamp,
    PRIMARY KEY (videoid)
);

// One-to-many from user point of view (lookup table)
CREATE TABLE user_videos (
    userid uuid,
    added_date timestamp,
    videoid uuid,
    name text,
    preview_image_location text,
    PRIMARY KEY (userid, added_date, videoid)
) WITH CLUSTERING ORDER BY (added_date DESC, videoid ASC);

// Track latest videos, grouped by day (if we ever develop a bad hotspot from the daily grouping here, we could mitigate by
// splitting the row using an arbitrary group number, making the partition key (yyyymmdd, group_number))
CREATE TABLE latest_videos (
    yyyymmdd text,
    added_date timestamp,
    videoid uuid,
    name text,
    preview_image_location text,
    PRIMARY KEY (yyyymmdd, added_date, videoid)
) WITH CLUSTERING ORDER BY (added_date DESC, videoid ASC);

// Video ratings (counter table)
CREATE TABLE video_ratings (
    videoid uuid,
    rating_counter counter,
    rating_total counter,
    PRIMARY KEY (videoid)
);

// Video ratings by user (to try and mitigate voting multiple times)
CREATE TABLE video_ratings_by_user (
    videoid uuid,
    userid uuid,
    rating int,
    PRIMARY KEY (videoid, userid)
);

// Index for tag keywords
CREATE TABLE videos_by_tag (
    tag text,
    videoid uuid,
    added_date timestamp,
    name text,
    preview_image_location text,
    tagged_date timestamp,
    PRIMARY KEY (tag, videoid)
);

// Index for tags by first letter in the tag
CREATE TABLE tags_by_letter (
    first_letter text,
    tag text,
    PRIMARY KEY (first_letter, tag)
);

// Comments for a given video
CREATE TABLE comments_by_video (
    videoid uuid,
    commentid timeuuid,
    userid uuid,
    comment text,
    PRIMARY KEY (videoid, commentid)
) WITH CLUSTERING ORDER BY (commentid DESC);

// Comments for a given user
CREATE TABLE comments_by_user (
    userid uuid,
    commentid timeuuid,
    videoid uuid,
    comment text,
    PRIMARY KEY (userid, commentid)
) WITH CLUSTERING ORDER BY (commentid DESC);

// Pending uploaded videos by id
CREATE TABLE uploaded_videos (
    videoid uuid,
    userid uuid,
    name text,
    description text,
    tags set<text>,
    added_date timestamp,
    jobid text,
    PRIMARY KEY (videoid)
);

// Same as uploaded_videos just keyed by the encoding job's id
CREATE TABLE uploaded_videos_by_jobid (
    jobid text,
    videoid uuid,
    userid uuid,
    name text,
    description text,
    tags set<text>,
    added_date timestamp,
    PRIMARY KEY (jobid)
);

// Log of notifications from Azure Media Services encoding jobs (latest updates first)
CREATE TABLE encoding_job_notifications (
    jobid text,
    status_date timestamp,
    etag text,
    newstate text,
    oldstate text,
    PRIMARY KEY (jobid, status_date, etag)
) WITH CLUSTERING ORDER BY (status_date DESC, etag ASC);
