create table courses
(
    course_id   int
        primary key,
    category_id int,
    video_count int
);

create table course_ratings
(
    rating_id integer
                  generated always as identity
        primary key,
    user_id   varchar,
    course_id integer
        references courses,
    rating    real
);

create table user_views
(
    view_id   integer
                  generated always as identity,
    user_id   varchar,
    course_id integer
        references courses,
    video_id  integer
);

insert into courses (course_id, category_id)
values (-1, 1),
       (-2, 2),
       (-3, 3),
       (-4, 4),
       (-5, 5),
       (-6, 6),
       (-7, 7),
       (-8, 8),
       (-9, 9),
       (-10, 10);

