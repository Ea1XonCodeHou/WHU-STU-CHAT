-- WHU Chat 数据库设计（含讨论区功能）
-- 版本: 2.0
-- 描述: 武汉大学学生交流平台完整数据库设计（含聊天和讨论区功能）

-- 创建数据库
CREATE DATABASE IF NOT EXISTS `whu-chat`;
USE `whu-chat`;

-- =====================================================
-- 基础用户和聊天功能表
-- =====================================================

-- 用户表 - 存储用户基本信息
CREATE TABLE Users (
    UserId INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(100) NOT NULL,         -- 直接存储加密后的密码
    Email VARCHAR(100),
    Phone VARCHAR(20),
    Avatar VARCHAR(255),
    Signature VARCHAR(200),                 -- 个人签名
    Status VARCHAR(20) DEFAULT 'offline',   -- 用户状态：online/offline
    IsStatusVisible BOOLEAN NOT NULL DEFAULT TRUE, -- 是否显示在线状态
    LastLoginTime DATETIME,                 -- 最后登录时间
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间
    UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP    -- 更新时间
);

-- 在用户名上建立索引
CREATE INDEX IX_Users_Username ON Users(Username);

-- 用户设置表
CREATE TABLE IF NOT EXISTS UserSettings (
    SettingId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    SettingKey VARCHAR(100) NOT NULL,
    SettingValue VARCHAR(255) NOT NULL,
    CreateTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdateTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    UNIQUE KEY (UserId, SettingKey)
);

-- 私聊消息表
CREATE TABLE PrivateMessages (
    MessageId INT PRIMARY KEY AUTO_INCREMENT,
    SenderId INT NOT NULL,
    ReceiverId INT NOT NULL,
    Content TEXT NOT NULL,
    IsRead TINYINT(1) DEFAULT 0,
    ReadTime DATETIME,                       -- 阅读时间
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间（发送时间）
    FOREIGN KEY (SenderId) REFERENCES Users(UserId),
    FOREIGN KEY (ReceiverId) REFERENCES Users(UserId)
);

-- 群组表
CREATE TABLE ChatGroups (
    GroupId INT PRIMARY KEY AUTO_INCREMENT,
    GroupName VARCHAR(100) NOT NULL,
    Description TEXT,
    CreatorId INT NOT NULL,
    MemberCount INT DEFAULT 1,               -- 成员数量
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间
    UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 更新时间
    FOREIGN KEY (CreatorId) REFERENCES Users(UserId)
);

-- 群组成员表
CREATE TABLE GroupMembers (
    MemberId INT PRIMARY KEY AUTO_INCREMENT,
    GroupId INT NOT NULL,
    UserId INT NOT NULL,
    JoinTime DATETIME DEFAULT CURRENT_TIMESTAMP,     -- 加入时间
    LastReadTime DATETIME,                   -- 最后阅读时间
    UNIQUE KEY UQ_GroupMembers (GroupId, UserId),
    FOREIGN KEY (GroupId) REFERENCES ChatGroups(GroupId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- 群聊消息表
CREATE TABLE GroupMessages (
    MessageId INT PRIMARY KEY AUTO_INCREMENT,
    GroupId INT NOT NULL,
    SenderId INT NOT NULL,
    Content TEXT NOT NULL,
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间（发送时间）
    FOREIGN KEY (GroupId) REFERENCES ChatGroups(GroupId),
    FOREIGN KEY (SenderId) REFERENCES Users(UserId)
);

-- 聊天室表
CREATE TABLE ChatRooms (
    RoomId INT PRIMARY KEY AUTO_INCREMENT,
    RoomName VARCHAR(100) NOT NULL,
    Description TEXT,
    CreatorId INT,                           -- 创建者ID
    ActiveUserCount INT DEFAULT 0,           -- 当前活跃用户数
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间
    UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 更新时间
    FOREIGN KEY (CreatorId) REFERENCES Users(UserId)
);

-- 聊天室消息表
CREATE TABLE RoomMessages (
    MessageId INT PRIMARY KEY AUTO_INCREMENT,
    RoomId INT NOT NULL,
    SenderId INT NOT NULL,
    Content TEXT NOT NULL,
    MessageType VARCHAR(20) DEFAULT 'text',  -- 消息类型：text, image, file, emoji等
    FileUrl VARCHAR(255),                    -- 文件URL，用于图片和文件消息
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间（发送时间）
    FOREIGN KEY (RoomId) REFERENCES ChatRooms(RoomId),
    FOREIGN KEY (SenderId) REFERENCES Users(UserId)
);

-- =====================================================
-- 讨论区相关表结构
-- =====================================================

-- 讨论区表
CREATE TABLE IF NOT EXISTS `discussion` (
  `DiscussionId` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(100) NOT NULL,
  `Description` VARCHAR(500) NOT NULL,
  `CreatorId` INT NOT NULL,
  `IsHot` TINYINT(1) NOT NULL DEFAULT 0,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  PRIMARY KEY (`DiscussionId`),
  INDEX `idx_creator` (`CreatorId`),
  INDEX `idx_update_time` (`UpdateTime`),
  FOREIGN KEY (`CreatorId`) REFERENCES `Users`(`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 帖子表
CREATE TABLE IF NOT EXISTS `post` (
  `PostId` INT NOT NULL AUTO_INCREMENT,
  `DiscussionId` INT NOT NULL,
  `Title` VARCHAR(200) NOT NULL,
  `Content` TEXT NOT NULL,
  `AuthorId` INT NOT NULL,
  `LikeCount` INT NOT NULL DEFAULT 0,
  `CommentCount` INT NOT NULL DEFAULT 0,
  `PostType` VARCHAR(20) NOT NULL DEFAULT 'normal',
  `IsAnonymous` BOOLEAN NOT NULL DEFAULT 0,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  PRIMARY KEY (`PostId`),
  INDEX `idx_discussion_id` (`DiscussionId`),
  INDEX `idx_author_id` (`AuthorId`),
  INDEX `idx_post_type` (`PostType`),
  INDEX `idx_update_time` (`UpdateTime`),
  FOREIGN KEY (`DiscussionId`) REFERENCES `discussion` (`DiscussionId`) ON DELETE CASCADE,
  FOREIGN KEY (`AuthorId`) REFERENCES `Users`(`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 帖子点赞表
CREATE TABLE IF NOT EXISTS `post_like` (
  `LikeId` INT NOT NULL AUTO_INCREMENT,
  `PostId` INT NOT NULL,
  `UserId` INT NOT NULL,
  `CreateTime` DATETIME NOT NULL,
  PRIMARY KEY (`LikeId`),
  UNIQUE KEY `idx_post_user` (`PostId`, `UserId`),
  INDEX `idx_user_id` (`UserId`),
  FOREIGN KEY (`PostId`) REFERENCES `post` (`PostId`) ON DELETE CASCADE,
  FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 评论表
CREATE TABLE IF NOT EXISTS `comment` (
  `CommentId` INT NOT NULL AUTO_INCREMENT,
  `PostId` INT NOT NULL,
  `Content` TEXT NOT NULL,
  `UserId` INT NOT NULL,
  `ParentId` INT NOT NULL DEFAULT 0,
  `LikeCount` INT NOT NULL DEFAULT 0,
  `IsAnonymous` BOOLEAN NOT NULL DEFAULT 0,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  PRIMARY KEY (`CommentId`),
  INDEX `idx_post_id` (`PostId`),
  INDEX `idx_user_id` (`UserId`),
  INDEX `idx_parent_id` (`ParentId`),
  INDEX `idx_create_time` (`CreateTime`),
  FOREIGN KEY (`PostId`) REFERENCES `post` (`PostId`) ON DELETE CASCADE,
  FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 初始化测试数据
-- =====================================================

-- 初始化一个测试用户
INSERT INTO Users (Username, Password, Email, Status, CreateTime, UpdateTime)
VALUES ('testuser', '123456', 'test@whu.edu.cn', 'online', NOW(), NOW());

-- 创建默认公共聊天室
INSERT INTO ChatRooms (RoomName, Description, CreatorId, CreateTime, UpdateTime)
VALUES ('WHU 校园公共聊天室', '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！', 1, NOW(), NOW());

-- 初始化讨论区数据
INSERT INTO `discussion` (`Title`, `Description`, `CreatorId`, `IsHot`, `CreateTime`, `UpdateTime`) VALUES
('校园生活', '讨论校园日常生活、活动和经验分享', 1, 1, NOW(), NOW()),
('学习交流', '学业问题、考试经验、学习方法分享', 1, 1, NOW(), NOW()),
('校园公告', '重要通知与公告', 1, 0, NOW(), NOW()),
('失物招领', '丢失和拾获物品信息发布', 1, 0, NOW(), NOW());

-- 初始化帖子数据
INSERT INTO `post` (`DiscussionId`, `Title`, `Content`, `AuthorId`, `PostType`, `IsAnonymous`, `CreateTime`, `UpdateTime`) VALUES
(1, '校园周边美食推荐', '最近发现了几家好吃的餐厅，推荐给大家...', 1, 'normal', 0, NOW(), NOW()),
(1, '周末活动征集', '有人周末想一起去爬山吗？', 1, 'normal', 0, NOW(), NOW()),
(2, '考研经验分享', '分享一下我备考的一些心得体会...', 1, 'sticky', 0, NOW(), NOW()),
(2, '有没有人一起组队参加比赛', '正在找队友参加下个月的编程比赛...', 1, 'normal', 0, NOW(), NOW());

-- 初始化评论数据
INSERT INTO `comment` (`PostId`, `Content`, `UserId`, `ParentId`, `LikeCount`, `IsAnonymous`, `CreateTime`, `UpdateTime`) VALUES
(1, '强烈推荐校门口的那家面馆！', 1, 0, 0, 0, NOW(), NOW()),
(1, '谢谢推荐，我去试试！', 1, 1, 0, 0, NOW(), NOW()),
(3, '感谢分享，请问英语是怎么准备的？', 1, 0, 0, 0, NOW(), NOW()),
(3, '我主要是刷真题和听课，详细可以私聊', 1, 3, 0, 0, NOW(), NOW());

-- 插入默认设置
INSERT IGNORE INTO UserSettings (UserId, SettingKey, SettingValue)
SELECT 
    UserId, 
    'setting_darkMode', 
    'false'
FROM 
    Users;

INSERT IGNORE INTO UserSettings (UserId, SettingKey, SettingValue)
SELECT 
    UserId, 
    'setting_messageSound', 
    'true'
FROM 
    Users;

INSERT IGNORE INTO UserSettings (UserId, SettingKey, SettingValue)
SELECT 
    UserId, 
    'setting_showMyOnlineStatus', 
    'true'
FROM 
    Users;

INSERT IGNORE INTO UserSettings (UserId, SettingKey, SettingValue)
SELECT 
    UserId, 
    'setting_newMessageNotification', 
    'true'
FROM 
    Users; 