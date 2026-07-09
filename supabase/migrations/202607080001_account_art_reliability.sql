update artist set subid = 'legacy-' || id::text where subid is null or subid = '';
update artist set name = 'Artist' || id::text where name is null or name = '';
update artist set privateprofile = false where privateprofile is null;
update artist set notificationsenabled = 63 where notificationsenabled is null;

alter table artist
  alter column subid set not null,
  alter column name set not null,
  alter column privateprofile set default false,
  alter column notificationsenabled set default 63;

create unique index if not exists ux_artist_subid on artist (subid);
create unique index if not exists ux_artist_name_lower on artist (lower(name));

update art set title = '' where title is null;
update art set width = 0 where width is null;
update art set height = 0 where height is null;
update art set encode = '' where encode is null;
update art set gifid = 0 where gifid is null;
update art set gifframenum = 0 where gifframenum is null;
update art set pointid = 0 where pointid is null;

alter table art
  alter column title set default '',
  alter column width set default 0,
  alter column height set default 0,
  alter column encode set default '',
  alter column gifid set default 0,
  alter column gifframenum set default 0,
  alter column pointid set default 0;
