-- Parent-owned learners. Auth passwords/emails remain in Supabase Auth, never in game profiles.
create schema if not exists private;
revoke all on schema private from public, anon;
grant usage on schema private to authenticated, service_role;
create table public.accounts (
 id uuid primary key references auth.users(id) on delete cascade,
 username text not null unique check (username ~ '^[a-z0-9_]{3,24}$'),
 created_at timestamptz not null default now()
);
create table public.learners (
 id uuid primary key default gen_random_uuid(), owner_id uuid not null references public.accounts(id) on delete cascade,
 nickname text not null default 'Space explorer' check (length(trim(nickname)) between 1 and 20),
 comparisons boolean not null default false, created_at timestamptz not null default now()
);
create index learners_owner on public.learners(owner_id);
create table public.learner_access (
 learner_id uuid references public.learners(id) on delete cascade, adult_id uuid references public.accounts(id) on delete cascade,
 access_role text not null check (access_role in ('parent','teacher')), primary key(learner_id, adult_id)
);
create table public.classrooms (
 id uuid primary key default gen_random_uuid(), teacher_id uuid not null references public.accounts(id) on delete cascade,
 name text not null check (length(trim(name)) between 1 and 60)
);
create index classrooms_teacher on public.classrooms(teacher_id);
create table public.class_members (
 class_id uuid references public.classrooms(id) on delete cascade, learner_id uuid references public.learners(id) on delete cascade,
 joined_at timestamptz not null default now(), primary key(class_id,learner_id)
);
create index class_members_learner on public.class_members(learner_id);
create table public.assignments (
 id uuid primary key default gen_random_uuid(), class_id uuid not null references public.classrooms(id) on delete cascade,
 topic int not null check(topic between 0 and 13), difficulty int not null check(difficulty between 0 and 2),
 target int not null check(target between 1 and 100), due_on date not null
);
create index assignments_class on public.assignments(class_id);
create table public.device_attempts (
 id text primary key check(length(id) = 32), learner_id uuid not null references public.learners(id) on delete cascade,
 recorded_at timestamptz not null default now(), event jsonb not null check(jsonb_typeof(event) = 'object' and octet_length(event::text) < 2048)
);
create index device_attempts_learner_date on public.device_attempts(learner_id, recorded_at desc);
create table private.challenges (
 id uuid primary key default gen_random_uuid(), learner_id uuid not null references public.learners(id) on delete cascade,
 topic int not null, difficulty int not null, prompt text not null, fact text not null, answer int not null,
 issued_at timestamptz not null default now(), used_at timestamptz
);
create index challenges_learner_date on private.challenges(learner_id, issued_at desc);
create table public.verified_attempts (
 challenge_id uuid primary key references private.challenges(id) on delete cascade,
 learner_id uuid not null references public.learners(id) on delete cascade, topic int not null, difficulty int not null,
 fact text not null, correct boolean not null, helped boolean not null,
 seconds numeric not null check(seconds >= 0), recorded_at timestamptz not null default now()
);
create index verified_attempts_learner_date on public.verified_attempts(learner_id,recorded_at desc);
create table private.auth_windows (bucket text not null, minute timestamptz not null, hits int not null, primary key(bucket,minute));

create function private.require_adult() returns uuid language plpgsql security definer set search_path = '' as $$
declare who uuid := auth.uid(); begin
 if who is null or not exists(select 1 from auth.users where id=who and email_confirmed_at is not null) then raise exception 'Verified adult sign-in required' using errcode='42501'; end if;
 return who;
end $$;
create function private.owns(learner uuid) returns boolean language sql stable security definer set search_path = '' as $$
 select exists(select 1 from public.learners l join auth.users u on u.id=auth.uid() where l.id=learner and l.owner_id=u.id and u.email_confirmed_at is not null)
$$;
create function private.can_read(learner uuid) returns boolean language sql stable security definer set search_path = '' as $$
 select private.owns(learner) or exists(select 1 from public.learner_access a join auth.users u on u.id=auth.uid() where a.learner_id=learner and a.adult_id=u.id and u.email_confirmed_at is not null)
 or exists(select 1 from public.class_members m join public.classrooms c on c.id=m.class_id join auth.users u on u.id=auth.uid() where m.learner_id=learner and c.teacher_id=u.id and u.email_confirmed_at is not null)
$$;
create function private.in_class(room uuid) returns boolean language sql stable security definer set search_path = '' as $$
 select exists(select 1 from public.classrooms c where c.id=room and c.teacher_id=auth.uid())
 or exists(select 1 from public.class_members m where m.class_id=room and private.owns(m.learner_id))
$$;
create function private.new_account() returns trigger language plpgsql security definer set search_path = '' as $$
declare handle text := lower(new.raw_user_meta_data->>'username'); begin
 if handle is null or handle !~ '^[a-z0-9_]{3,24}$' then handle := 'parent_' || substr(replace(new.id::text,'-',''),1,12); end if;
 insert into public.accounts(id,username) values(new.id,handle);
 insert into public.learners(owner_id) values(new.id);
 return new;
end $$;
create trigger create_game_account after insert on auth.users for each row execute function private.new_account();

alter table public.accounts enable row level security;
alter table public.learners enable row level security;
alter table public.learner_access enable row level security;
alter table public.classrooms enable row level security;
alter table public.class_members enable row level security;
alter table public.assignments enable row level security;
alter table public.device_attempts enable row level security;
alter table public.verified_attempts enable row level security;
alter table private.challenges enable row level security;
alter table private.auth_windows enable row level security;
create policy self_account on public.accounts for select to authenticated using(id=(select auth.uid()));
create policy linked_learner on public.learners for select to authenticated using(private.can_read(id));
create policy linked_access on public.learner_access for select to authenticated using(adult_id=(select auth.uid()) or private.owns(learner_id));
create policy linked_class on public.classrooms for select to authenticated using(private.in_class(id));
create policy linked_members on public.class_members for select to authenticated using(private.in_class(class_id));
create policy linked_assignments on public.assignments for select to authenticated using(private.in_class(class_id));
create policy teacher_assignments on public.assignments for insert to authenticated with check(exists(select 1 from public.classrooms where id=class_id and teacher_id=(select auth.uid())));
create policy teacher_update_assignments on public.assignments for update to authenticated using(exists(select 1 from public.classrooms where id=class_id and teacher_id=(select auth.uid()))) with check(exists(select 1 from public.classrooms where id=class_id and teacher_id=(select auth.uid())));
create policy linked_device_data on public.device_attempts for select to authenticated using(private.can_read(learner_id));
create policy linked_verified_data on public.verified_attempts for select to authenticated using(private.can_read(learner_id));
revoke all on all tables in schema public from anon, authenticated;
grant select on public.accounts, public.learners, public.learner_access, public.classrooms, public.class_members, public.assignments, public.device_attempts, public.verified_attempts to authenticated;
grant insert, update on public.assignments to authenticated;

-- All privileged mutations check the signed-in actor; clients cannot choose roles, ownership or scores.
create function private.account_state() returns jsonb language plpgsql security definer set search_path='' as $$
declare who uuid:=private.require_adult(); begin
 return jsonb_build_object('username',(select username from public.accounts where id=who),'learners',coalesce((select jsonb_agg(jsonb_build_object('id',id,'nickname',nickname,'comparisons',comparisons)) from public.learners where owner_id=who),'[]'::jsonb));
end $$;
create function public.account_state() returns jsonb language sql security invoker set search_path='' as $$ select private.account_state() $$;
create function private.rename_learner(learner uuid,nickname text) returns void language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Not allowed' using errcode='42501'; end if; update public.learners set nickname=trim(rename_learner.nickname) where id=learner; end $$;
create function public.rename_learner(learner uuid,nickname text) returns void language sql security invoker set search_path='' as $$ select private.rename_learner(learner,nickname) $$;
create function private.rename_account(username text) returns void language plpgsql security definer set search_path='' as $$
begin update public.accounts set username=lower(trim(rename_account.username)) where id=private.require_adult(); end $$;
create function public.rename_account(username text) returns void language sql security invoker set search_path='' as $$ select private.rename_account(username) $$;
create function private.set_comparisons(learner uuid,enabled boolean) returns void language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Parent required' using errcode='42501'; end if; update public.learners set comparisons=enabled where id=learner; end $$;
create function public.set_comparisons(learner uuid,enabled boolean) returns void language sql security invoker set search_path='' as $$ select private.set_comparisons(learner,enabled) $$;
create function private.create_class(name text) returns uuid language plpgsql security definer set search_path='' as $$
declare room uuid; begin insert into public.classrooms(teacher_id,name) values(private.require_adult(),trim(name)) returning id into room; return room; end $$;
create function public.create_class(name text) returns uuid language sql security invoker set search_path='' as $$ select private.create_class(name) $$;
create function private.join_class(learner uuid,code uuid) returns uuid language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Parent required' using errcode='42501'; end if;
 if not exists(select 1 from public.classrooms where id=code) then raise exception 'Invalid class code'; end if;
 insert into public.class_members(class_id,learner_id) values(code,learner) on conflict do nothing; return code; end $$;
create function public.join_class(learner uuid,code uuid) returns uuid language sql security invoker set search_path='' as $$ select private.join_class(learner,code) $$;
create function private.leave_class(learner uuid,room uuid) returns void language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Parent required' using errcode='42501'; end if; delete from public.class_members where class_id=room and learner_id=learner; end $$;
create function public.leave_class(learner uuid,room uuid) returns void language sql security invoker set search_path='' as $$ select private.leave_class(learner,room) $$;
create function private.link_adult(learner uuid,email text,kind text) returns void language plpgsql security definer set search_path='' as $$
declare adult uuid; begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Parent required' using errcode='42501'; end if;
 select id into adult from auth.users where lower(auth.users.email)=lower(link_adult.email) and email_confirmed_at is not null;
 if adult is null then raise exception 'The adult must create and confirm an account first'; end if;
 insert into public.learner_access values(learner,adult,kind) on conflict(learner_id,adult_id) do update set access_role=excluded.access_role;
end $$;
create function public.link_adult(learner uuid,email text,kind text) returns void language sql security invoker set search_path='' as $$ select private.link_adult(learner,email,kind) $$;
create function private.revoke_adult(learner uuid,adult uuid) returns void language plpgsql security definer set search_path='' as $$
begin if not private.owns(learner) then raise exception 'Parent required' using errcode='42501'; end if; delete from public.learner_access where learner_id=learner and adult_id=adult; end $$;
create function public.revoke_adult(learner uuid,adult uuid) returns void language sql security invoker set search_path='' as $$ select private.revoke_adult(learner,adult) $$;
create function private.record_device_attempt(id text,learner uuid,event jsonb) returns void language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Not allowed' using errcode='42501'; end if;
 if not (event ?& array['Topic','Difficulty','Kind','FirstTry','Correct','Hint','Model','Seconds']) or jsonb_typeof(event->'Topic')<>'number' or jsonb_typeof(event->'Difficulty')<>'number' or jsonb_typeof(event->'Seconds')<>'number' or jsonb_typeof(event->'FirstTry')<>'boolean' or jsonb_typeof(event->'Correct')<>'boolean' or jsonb_typeof(event->'Hint')<>'boolean' or jsonb_typeof(event->'Model')<>'boolean' then raise exception 'Invalid event fields'; end if;
 if (event->>'Seconds')::numeric not between 0 and 3600 then raise exception 'Invalid duration'; end if;
 if (event->>'Topic')::int not between 0 and 13 or (event->>'Difficulty')::int not between 0 and 2 or coalesce(event->>'Kind','') not in ('answer','demonstration') then raise exception 'Invalid event'; end if;
 insert into public.device_attempts(id,learner_id,event) values(id,learner,event) on conflict do nothing;
end $$;
create function public.record_device_attempt(id text,learner uuid,event jsonb) returns void language sql security invoker set search_path='' as $$ select private.record_device_attempt(id,learner,event) $$;

-- Competitive questions are issued and graded by the server; offline/device events never enter rankings.
create function private.new_challenge(learner uuid,topic int,difficulty int) returns jsonb language plpgsql security definer set search_path='' as $$
declare a int; b int; answer int; total int; family int; lim int; op text:='+'; prompt text; missing boolean:=false; question_id uuid;
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Not allowed' using errcode='42501'; end if;
 if topic not between 0 and 13 or difficulty not between 0 and 2 then raise exception 'Invalid topic or difficulty'; end if;
 if (select count(*) from private.challenges c where c.learner_id=learner and issued_at>now()-interval '1 minute')>=30 then raise exception 'Take a short break before another question'; end if;
 lim:=case difficulty when 0 then 10 when 1 then 20 else 100 end;
 family:=case topic when 5 then floor(random()*4)::int when 1 then 1 when 3 then 2 when 4 then 3 else 0 end;
 if topic in (2,6,7,13) then
  missing:=true; total:=case topic when 2 then 10 when 6 then 20 when 7 then 0 else 2+floor(random()*(lim-1))::int end;
  if topic=7 then a:=1+floor(random()*(case difficulty when 0 then 5 when 1 then 10 else 20 end))::int; b:=a; op:='−';
  else a:=floor(random()*(total+1))::int; b:=total-a; end if; answer:=b;
 elsif topic in (8,9) then a:=1+floor(random()*(case difficulty when 0 then 5 when 1 then 10 else 20 end))::int; b:=a+case topic when 9 then 1 else 0 end; answer:=a+b; total:=answer;
 elsif topic in (10,11,12) then b:=case topic when 10 then 2 when 11 then 5 else 10 end; a:=(1+floor(random()*(case difficulty when 0 then 5 when 1 then 10 else 20 end))::int)*b; answer:=a+b; total:=answer;
 elsif family=0 then a:=floor(random()*lim)::int; b:=floor(random()*(lim-a+1))::int; answer:=a+b; total:=answer;
 elsif family=1 then a:=1+floor(random()*lim)::int; b:=floor(random()*(a+1))::int; answer:=a-b; total:=answer; op:='−';
 elsif family=2 then a:=1+floor(random()*(case difficulty when 0 then 5 when 1 then 10 else 12 end))::int; b:=1+floor(random()*(case difficulty when 0 then 3 when 1 then 10 else 12 end))::int; answer:=a*b; total:=answer; op:='×';
 else b:=1+floor(random()*(case difficulty when 0 then 5 when 1 then 10 else 12 end))::int; answer:=1+floor(random()*(case difficulty when 0 then 3 when 1 then 10 else 12 end))::int; a:=b*answer; total:=answer; op:='÷'; end if;
 prompt:=case when topic in(10,11,12) then (a-b)||', '||a||', ?' when missing then a||' '||op||' ? = '||total else a||' '||op||' '||b||' = ?' end;
 insert into private.challenges(learner_id,topic,difficulty,prompt,fact,answer) values(learner,topic,difficulty,prompt,topic||':'||a||op||b,answer) returning id into question_id;
 return jsonb_build_object('id',question_id,'prompt',prompt);
end $$;
create function public.new_challenge(learner uuid,topic int,difficulty int) returns jsonb language sql security invoker set search_path='' as $$ select private.new_challenge(learner,topic,difficulty) $$;
create function private.answer_challenge(challenge uuid,value int,helped boolean) returns jsonb language plpgsql security definer set search_path='' as $$
declare q private.challenges; ok boolean; begin perform private.require_adult(); select * into q from private.challenges where id=challenge for update;
 if q.id is null or not private.owns(q.learner_id) then raise exception 'Not allowed' using errcode='42501'; end if;
 if q.used_at is not null or q.issued_at<now()-interval '10 minutes' then raise exception 'Question already answered or expired'; end if;
 ok:=q.answer=value; update private.challenges set used_at=now() where id=q.id;
 insert into public.verified_attempts values(q.id,q.learner_id,q.topic,q.difficulty,q.fact,ok,helped,greatest(0,extract(epoch from now()-q.issued_at)),now());
 return jsonb_build_object('correct',ok,'answer',q.answer,'points',case when ok and not helped then 1 else 0 end);
end $$;
create function public.answer_challenge(challenge uuid,value int,helped boolean default false) returns jsonb language sql security invoker set search_path='' as $$ select private.answer_challenge(challenge,value,helped) $$;
create function private.class_board(room uuid,topic int,difficulty int) returns jsonb language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.in_class(room) then raise exception 'Not your class' using errcode='42501'; end if;
 return jsonb_build_object('message','This week · same topic and difficulty · max 20 distinct facts per day','rows',coalesce((
  with unique_facts as (select distinct v.learner_id,v.fact,(v.recorded_at at time zone 'UTC')::date fact_day from public.verified_attempts v join public.class_members m on m.learner_id=v.learner_id join public.learners l on l.id=v.learner_id where m.class_id=room and l.comparisons and v.topic=class_board.topic and v.difficulty=class_board.difficulty and v.correct and not v.helped and v.recorded_at>=date_trunc('week',now() at time zone 'UTC') at time zone 'UTC'),
  capped as(select *,row_number() over(partition by learner_id,fact_day order by fact) n from unique_facts),
  totals as(select learner_id,count(*)::int points from capped where n<=20 group by learner_id),
  rows as(select l.nickname,coalesce(t.points,0) points,coalesce(t.points,0) questions from public.learners l join public.class_members m on m.learner_id=l.id left join totals t on t.learner_id=l.id where m.class_id=room and l.comparisons order by coalesce(t.points,0) desc,l.nickname limit 100)
  select jsonb_agg(to_jsonb(rows)) from rows),'[]'::jsonb));
end $$;
create function public.class_board(room uuid,topic int,difficulty int) returns jsonb language sql security invoker set search_path='' as $$ select private.class_board(room,topic,difficulty) $$;
create function private.dashboard_data() returns jsonb language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); return jsonb_build_object(
 'learners',coalesce((select jsonb_agg(to_jsonb(l)||jsonb_build_object('owner',private.owns(l.id))) from public.learners l where private.can_read(l.id)),'[]'::jsonb),
 'classes',coalesce((select jsonb_agg(to_jsonb(c)) from public.classrooms c where private.in_class(c.id)),'[]'::jsonb),
 'accesses',coalesce((select jsonb_agg(jsonb_build_object('learner',a.learner_id,'adult',a.adult_id,'username',u.username,'kind',a.access_role)) from public.learner_access a join public.accounts u on u.id=a.adult_id where private.owns(a.learner_id)),'[]'::jsonb),
 'games',coalesce((select jsonb_agg(to_jsonb(g)) from public.game_snapshots g where private.can_read(g.learner_id)),'[]'::jsonb),
 'assignments',coalesce((select jsonb_agg(to_jsonb(a)) from public.assignments a where private.in_class(a.class_id)),'[]'::jsonb),
 'topics',coalesce((select jsonb_agg(to_jsonb(s)) from (
 select learner_id,(event->>'Topic')::int topic,(event->>'Difficulty')::int difficulty,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean) first_checks,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and (event->>'Correct')::boolean) correct_first,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and ((event->>'Hint')::boolean or (event->>'Model')::boolean)) helped_checks,
 count(*) filter(where event->>'Kind'='demonstration') demonstrations,
 percentile_cont(.5) within group(order by (event->>'Seconds')::numeric) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and (event->>'Correct')::boolean and (event->>'Seconds')::numeric>0 and (event->>'Seconds')::numeric<=180) median_seconds,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and recorded_at>=now()-interval '7 days') recent_checks,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and (event->>'Correct')::boolean and recorded_at>=now()-interval '7 days') recent_correct,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and recorded_at<now()-interval '7 days' and recorded_at>=now()-interval '14 days') previous_checks,
 count(*) filter(where event->>'Kind'='answer' and (event->>'FirstTry')::boolean and (event->>'Correct')::boolean and recorded_at<now()-interval '7 days' and recorded_at>=now()-interval '14 days') previous_correct
 from public.device_attempts where private.can_read(learner_id) group by learner_id,(event->>'Topic')::int,(event->>'Difficulty')::int
 )s),'[]'::jsonb)); end $$;
create function public.dashboard_data() returns jsonb language sql security invoker set search_path='' as $$ select private.dashboard_data() $$;

-- Service-only helpers for the rate-limited account gateway. Never exposed to game clients.
create function public.account_email(handle text) returns text language sql security definer set search_path='' as $$ select u.email from auth.users u join public.accounts a on a.id=u.id where a.username=lower(handle) $$;
create function public.allow_auth(bucket text) returns boolean language plpgsql security definer set search_path='' as $$
declare n int; begin
 delete from private.auth_windows where minute<now()-interval '1 day';
 insert into private.auth_windows values(bucket,date_trunc('minute',now()),1) on conflict(bucket,minute) do update set hits=private.auth_windows.hits+1 returning hits into n;
 return n<=8;
end $$;
revoke execute on all functions in schema private from public,anon,authenticated;
revoke execute on all functions in schema public from public,anon,authenticated;
grant execute on function private.require_adult(),private.owns(uuid),private.can_read(uuid),private.in_class(uuid),private.account_state(),private.rename_learner(uuid,text),private.rename_account(text),private.set_comparisons(uuid,boolean),private.create_class(text),private.join_class(uuid,uuid),private.leave_class(uuid,uuid),private.link_adult(uuid,text,text),private.revoke_adult(uuid,uuid),private.record_device_attempt(text,uuid,jsonb),private.new_challenge(uuid,int,int),private.answer_challenge(uuid,int,boolean),private.class_board(uuid,int,int),private.dashboard_data() to authenticated;
grant execute on function public.account_state(),public.rename_learner(uuid,text),public.rename_account(text),public.set_comparisons(uuid,boolean),public.create_class(text),public.join_class(uuid,uuid),public.leave_class(uuid,uuid),public.link_adult(uuid,text,text),public.revoke_adult(uuid,uuid),public.record_device_attempt(text,uuid,jsonb),public.new_challenge(uuid,int,int),public.answer_challenge(uuid,int,boolean),public.class_board(uuid,int,int),public.dashboard_data() to authenticated;
grant execute on function public.account_email(text),public.allow_auth(text) to service_role;

create table public.game_snapshots (
 learner_id uuid primary key references public.learners(id) on delete cascade,
 updated_at timestamptz not null default now(), snapshot jsonb not null check(octet_length(snapshot::text)<8192)
);
alter table public.game_snapshots enable row level security;
revoke all on public.game_snapshots from anon, authenticated;
grant select on public.game_snapshots to authenticated;
create policy linked_game_snapshot on public.game_snapshots for select to authenticated using(private.can_read(learner_id));
create function private.save_game_snapshot(learner uuid,snapshot jsonb) returns void language plpgsql security definer set search_path='' as $$
begin perform private.require_adult(); if not private.owns(learner) then raise exception 'Not allowed' using errcode='42501'; end if;
 if jsonb_typeof(snapshot)<>'object' or not (snapshot ?& array['Night','Level','Kills','Upgrades','Health','Shield']) then raise exception 'Invalid snapshot'; end if;
 insert into public.game_snapshots values(learner,now(),snapshot) on conflict(learner_id) do update set updated_at=excluded.updated_at,snapshot=excluded.snapshot;
end $$;
create function public.save_game_snapshot(learner uuid,snapshot jsonb) returns void language sql security invoker set search_path='' as $$ select private.save_game_snapshot(learner,snapshot) $$;
revoke execute on function public.save_game_snapshot(uuid,jsonb),private.save_game_snapshot(uuid,jsonb) from public,anon;
grant execute on function public.save_game_snapshot(uuid,jsonb),private.save_game_snapshot(uuid,jsonb) to authenticated;

create index learner_access_adult on public.learner_access(adult_id);
