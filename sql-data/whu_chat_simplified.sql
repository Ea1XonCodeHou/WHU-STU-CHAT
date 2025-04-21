-- WHU Chat 简化版数据库设计（MySQL版）
-- 版本: 1.0
-- 描述: 武汉大学学生交流平台聊天功能数据库设计（简化版）

-- 创建数据库
CREATE DATABASE IF NOT EXISTS `whu-chat`;
USE `whu-chat`;

-- 用户表 - 存储用户基本信息
CREATE TABLE Users (
    UserId INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(100) NOT NULL,         -- 直接存储加密后的密码
    Email VARCHAR(100),
    Phone VARCHAR(20),
    Avatar VARCHAR(255),
    LastLoginTime DATETIME,                  -- 最后登录时间
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间
    UpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP    -- 更新时间
);

-- 在用户名上建立索引
CREATE INDEX IX_Users_Username ON Users(Username);

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
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,   -- 创建时间（发送时间）
    FOREIGN KEY (RoomId) REFERENCES ChatRooms(RoomId),
    FOREIGN KEY (SenderId) REFERENCES Users(UserId)
);

-- 初始化一个测试用户
INSERT INTO Users (Username, Password, Email, CreateTime, UpdateTime)
VALUES ('testuser', '123456', 'test@whu.edu.cn', NOW(), NOW());

-- 创建默认公共聊天室
INSERT INTO ChatRooms (RoomName, Description, CreatorId, CreateTime, UpdateTime)
VALUES ('WHU 校园公共聊天室', '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！', 1, NOW(), NOW()); 