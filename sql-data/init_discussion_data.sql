-- Check if there is no data in Discussions table, if so, add initial data
INSERT INTO Discussions (Title, Description, CreatorId, CreateTime, UpdateTime, PostCount)
SELECT 'General Discussion', 'Welcome to the general discussion area', 1, NOW(), NOW(), 0
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Discussions LIMIT 1);

-- Check if there is no data in Posts table, if so, add initial post
INSERT INTO Posts (DiscussionId, Title, Content, AuthorId, IsAnonymous, CreateTime, UpdateTime, ViewCount, LikeCount, CommentCount, PostType)
SELECT 1, 'Welcome to WHU Student Chat', 'This is a platform for students to exchange learning experiences and campus life.\n\nHope you can find like-minded friends here.', 1, 0, NOW(), NOW(), 0, 0, 0, 'normal'
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Posts LIMIT 1);

-- Add a discussion area "Learning Exchange"
INSERT INTO Discussions (Title, Description, CreatorId, CreateTime, UpdateTime, PostCount)
SELECT 'Learning Exchange', 'Share learning experiences and discuss academic issues', 1, NOW(), NOW(), 0
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Discussions WHERE Title = 'Learning Exchange');

-- Add a discussion area "Campus Life"
INSERT INTO Discussions (Title, Description, CreatorId, CreateTime, UpdateTime, PostCount)
SELECT 'Campus Life', 'Share campus stories, food recommendations and other life topics', 1, NOW(), NOW(), 0
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Discussions WHERE Title = 'Campus Life');

-- Add a discussion area "Career"
INSERT INTO Discussions (Title, Description, CreatorId, CreateTime, UpdateTime, PostCount)
SELECT 'Career', 'Share internship and job experiences', 1, NOW(), NOW(), 0
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Discussions WHERE Title = 'Career');

-- Add some initial posts to different discussion areas
INSERT INTO Posts (DiscussionId, Title, Content, AuthorId, IsAnonymous, CreateTime, UpdateTime, ViewCount, LikeCount, CommentCount, PostType)
SELECT (SELECT DiscussionId FROM Discussions WHERE Title = 'Learning Exchange'), 
       'How to study efficiently for finals?', 
       'Finals are coming up, does anyone have efficient study methods to share? I personally think making a plan is important.', 
       1, 0, NOW(), NOW(), 5, 2, 0, 'normal'
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = 'How to study efficiently for finals?');

INSERT INTO Posts (DiscussionId, Title, Content, AuthorId, IsAnonymous, CreateTime, UpdateTime, ViewCount, LikeCount, CommentCount, PostType)
SELECT (SELECT DiscussionId FROM Discussions WHERE Title = 'Campus Life'), 
       'Recommended restaurants near campus', 
       'As a foodie, I want to recommend several restaurants near campus:\n1. Noodle shop near the lake\n2. Lanzhou ramen near engineering department\n3. Sichuan cuisine in south campus\nShare your favorites too!', 
       1, 0, NOW(), NOW(), 8, 3, 0, 'normal'
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = 'Recommended restaurants near campus');

INSERT INTO Posts (DiscussionId, Title, Content, AuthorId, IsAnonymous, CreateTime, UpdateTime, ViewCount, LikeCount, CommentCount, PostType)
SELECT (SELECT DiscussionId FROM Discussions WHERE Title = 'Career'), 
       'Summer internship experience', 
       'Just finished my summer internship, want to share my experience. First, resume is very important, highlight your project experience and professional skills. Second, learn about the company and position before the interview. Finally, learn a lot during the internship.', 
       1, 0, NOW(), NOW(), 12, 5, 0, 'normal'
FROM dual
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = 'Summer internship experience'); 