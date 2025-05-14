/*
 Navicat Premium Data Transfer

 Source Server         : HycMysql
 Source Server Type    : MySQL
 Source Server Version : 80033 (8.0.33)
 Source Host           : localhost:3306
 Source Schema         : whu-chat

 Target Server Type    : MySQL
 Target Server Version : 80033 (8.0.33)
 File Encoding         : 65001

 Date: 13/05/2025 12:36:29
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for aichathistory
-- ----------------------------
DROP TABLE IF EXISTS `aichathistory`;
CREATE TABLE `aichathistory`  (
  `ChatId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `SessionId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `UserMessage` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `AIResponse` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`ChatId`) USING BTREE,
  INDEX `idx_user_id`(`UserId` ASC) USING BTREE,
  INDEX `idx_session_id`(`SessionId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `aichathistory_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of aichathistory
-- ----------------------------

-- ----------------------------
-- Table structure for aichatsummary
-- ----------------------------
DROP TABLE IF EXISTS `aichatsummary`;
CREATE TABLE `aichatsummary`  (
  `SummaryId` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NULL DEFAULT NULL,
  `RoomId` int NULL DEFAULT NULL,
  `Summary` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `StartTime` datetime NOT NULL,
  `EndTime` datetime NOT NULL,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`SummaryId`) USING BTREE,
  INDEX `idx_group_id`(`GroupId` ASC) USING BTREE,
  INDEX `idx_room_id`(`RoomId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `aichatsummary_ibfk_1` FOREIGN KEY (`GroupId`) REFERENCES `chatgroups` (`GroupId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `aichatsummary_ibfk_2` FOREIGN KEY (`RoomId`) REFERENCES `chatrooms` (`RoomId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of aichatsummary
-- ----------------------------

-- ----------------------------
-- Table structure for chatgroups
-- ----------------------------
DROP TABLE IF EXISTS `chatgroups`;
CREATE TABLE `chatgroups`  (
  `GroupId` int NOT NULL AUTO_INCREMENT,
  `GroupName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
  `GroupAvatar` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `CreatorId` int NOT NULL,
  `MemberCount` int NULL DEFAULT 1,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `IsPrivate` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`GroupId`) USING BTREE,
  INDEX `CreatorId`(`CreatorId` ASC) USING BTREE,
  CONSTRAINT `chatgroups_ibfk_1` FOREIGN KEY (`CreatorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of chatgroups
-- ----------------------------
INSERT INTO `chatgroups` VALUES (1, 'Mike和Eaxon的私聊', '私聊', NULL, 2, 2, '2025-05-08 03:56:05', '2025-05-08 11:56:04', 0);
INSERT INTO `chatgroups` VALUES (2, 'Eaxon和shiro的私聊', '私聊', NULL, 3, 1, '2025-05-11 09:00:11', '2025-05-11 17:00:11', 0);

-- ----------------------------
-- Table structure for chatrooms
-- ----------------------------
DROP TABLE IF EXISTS `chatrooms`;
CREATE TABLE `chatrooms`  (
  `RoomId` int NOT NULL AUTO_INCREMENT,
  `RoomName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
  `CreatorId` int NULL DEFAULT NULL,
  `ActiveUserCount` int NULL DEFAULT 0,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`RoomId`) USING BTREE,
  INDEX `CreatorId`(`CreatorId` ASC) USING BTREE,
  CONSTRAINT `chatrooms_ibfk_1` FOREIGN KEY (`CreatorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 6 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of chatrooms
-- ----------------------------
INSERT INTO `chatrooms` VALUES (1, 'WHU 校园公共聊天室', '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `chatrooms` VALUES (2, '交友聊天室', '在这里找到志同道合的朋友，扩展你的社交圈', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `chatrooms` VALUES (3, '学术交流厅', '讨论学术问题，分享研究心得和学习资源', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `chatrooms` VALUES (4, '游戏玩家俱乐部', '讨论热门游戏，组队开黑，分享游戏攻略', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `chatrooms` VALUES (5, '求职交流区', '分享求职经验，讨论职业规划，互通招聘信息', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');

-- ----------------------------
-- Table structure for comment
-- ----------------------------
DROP TABLE IF EXISTS `comment`;
CREATE TABLE `comment`  (
  `CommentId` int NOT NULL AUTO_INCREMENT,
  `PostId` int NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `UserId` int NOT NULL,
  `ParentId` int NOT NULL DEFAULT 0,
  `LikeCount` int NOT NULL DEFAULT 0,
  `IsAnonymous` tinyint(1) NOT NULL DEFAULT 0,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CommentId`) USING BTREE,
  INDEX `idx_post_id`(`PostId` ASC) USING BTREE,
  INDEX `idx_user_id`(`UserId` ASC) USING BTREE,
  INDEX `idx_parent_id`(`ParentId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `comment_ibfk_1` FOREIGN KEY (`PostId`) REFERENCES `posts` (`PostId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `comment_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 9 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of comment
-- ----------------------------
INSERT INTO `comment` VALUES (1, 1, '强烈推荐校门口的那家面馆！', 1, 0, 0, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `comment` VALUES (2, 1, '谢谢推荐，我去试试！', 1, 1, 0, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `comment` VALUES (3, 3, '感谢分享，请问英语是怎么准备的？', 1, 0, 0, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `comment` VALUES (4, 3, '我主要是刷真题和听课，详细可以私聊', 1, 3, 0, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `comment` VALUES (5, 3, 'llll', 3, 0, 0, 0, '2025-05-08 11:13:19', '2025-05-08 11:13:19');
INSERT INTO `comment` VALUES (6, 5, '有人吗', 2, 0, 0, 0, '2025-05-08 11:18:25', '2025-05-08 11:18:25');
INSERT INTO `comment` VALUES (7, 4, '你谁啊？', 2, 0, 0, 0, '2025-05-08 11:54:47', '2025-05-08 11:54:47');
INSERT INTO `comment` VALUES (8, 6, '不知道啊，好难！', 2, 0, 0, 0, '2025-05-09 16:52:20', '2025-05-09 16:52:20');

-- ----------------------------
-- Table structure for discussion
-- ----------------------------
DROP TABLE IF EXISTS `discussion`;
CREATE TABLE `discussion`  (
  `DiscussionId` int NOT NULL AUTO_INCREMENT,
  `Title` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatorId` int NOT NULL,
  `IsHot` tinyint(1) NOT NULL DEFAULT 0,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`DiscussionId`) USING BTREE,
  INDEX `idx_creator`(`CreatorId` ASC) USING BTREE,
  INDEX `idx_update_time`(`UpdateTime` ASC) USING BTREE,
  CONSTRAINT `discussion_ibfk_1` FOREIGN KEY (`CreatorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 5 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of discussion
-- ----------------------------
INSERT INTO `discussion` VALUES (1, '校园生活', '讨论校园日常生活、活动和经验分享', 1, 1, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `discussion` VALUES (2, '学习交流', '学业问题、考试经验、学习方法分享', 1, 1, '2025-05-08 10:55:01', '2025-05-09 16:52:20');
INSERT INTO `discussion` VALUES (3, '校园公告', '重要通知与公告', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `discussion` VALUES (4, '失物招领', '丢失和拾获物品信息发布', 1, 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');

-- ----------------------------
-- Table structure for files
-- ----------------------------
DROP TABLE IF EXISTS `files`;
CREATE TABLE `files`  (
  `FileId` int NOT NULL AUTO_INCREMENT,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `FilePath` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `FileSize` bigint NOT NULL,
  `FileType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `UploaderId` int NOT NULL,
  `UploadTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`FileId`) USING BTREE,
  INDEX `idx_uploader`(`UploaderId` ASC) USING BTREE,
  INDEX `idx_upload_time`(`UploadTime` ASC) USING BTREE,
  CONSTRAINT `files_ibfk_1` FOREIGN KEY (`UploaderId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of files
-- ----------------------------

-- ----------------------------
-- Table structure for friendships
-- ----------------------------
DROP TABLE IF EXISTS `friendships`;
CREATE TABLE `friendships`  (
  `FriendshipId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `FriendId` int NOT NULL,
  `Status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'pending',
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`FriendshipId`) USING BTREE,
  UNIQUE INDEX `UserId`(`UserId` ASC, `FriendId` ASC) USING BTREE,
  INDEX `FriendId`(`FriendId` ASC) USING BTREE,
  CONSTRAINT `friendships_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `friendships_ibfk_2` FOREIGN KEY (`FriendId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of friendships
-- ----------------------------
INSERT INTO `friendships` VALUES (1, 3, 5, 'accepted', '2025-05-11 08:59:56', '2025-05-11 09:00:11');
INSERT INTO `friendships` VALUES (2, 5, 3, 'accepted', '2025-05-11 09:00:11', '2025-05-11 09:00:11');

-- ----------------------------
-- Table structure for groupmembers
-- ----------------------------
DROP TABLE IF EXISTS `groupmembers`;
CREATE TABLE `groupmembers`  (
  `MemberId` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `UserId` int NOT NULL,
  `Role` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT 'member',
  `JoinTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `LastReadTime` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`MemberId`) USING BTREE,
  UNIQUE INDEX `UQ_GroupMembers`(`GroupId` ASC, `UserId` ASC) USING BTREE,
  INDEX `UserId`(`UserId` ASC) USING BTREE,
  CONSTRAINT `groupmembers_ibfk_1` FOREIGN KEY (`GroupId`) REFERENCES `chatgroups` (`GroupId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `groupmembers_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 5 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of groupmembers
-- ----------------------------
INSERT INTO `groupmembers` VALUES (1, 1, 2, 'member', '2025-05-08 03:56:05', NULL);
INSERT INTO `groupmembers` VALUES (2, 1, 3, 'member', '2025-05-08 03:56:05', NULL);
INSERT INTO `groupmembers` VALUES (3, 2, 3, 'member', '2025-05-11 09:00:11', NULL);

-- ----------------------------
-- Table structure for groupmessages
-- ----------------------------
DROP TABLE IF EXISTS `groupmessages`;
CREATE TABLE `groupmessages`  (
  `MessageId` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `SenderId` int NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
  `MessageType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT 'text',
  `FileUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileSize` bigint NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`MessageId`) USING BTREE,
  INDEX `SenderId`(`SenderId` ASC) USING BTREE,
  INDEX `idx_group_id`(`GroupId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `groupmessages_ibfk_1` FOREIGN KEY (`GroupId`) REFERENCES `chatgroups` (`GroupId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `groupmessages_ibfk_2` FOREIGN KEY (`SenderId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 5 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of groupmessages
-- ----------------------------
INSERT INTO `groupmessages` VALUES (1, 1, 2, '说话!', 'text', NULL, NULL, NULL, '2025-05-09 08:50:53');
INSERT INTO `groupmessages` VALUES (2, 1, 3, '咋了', 'text', NULL, NULL, NULL, '2025-05-09 08:53:59');
INSERT INTO `groupmessages` VALUES (3, 1, 3, '🤣', 'text', NULL, NULL, NULL, '2025-05-09 08:58:58');
INSERT INTO `groupmessages` VALUES (4, 1, 3, '😍', 'text', NULL, NULL, NULL, '2025-05-09 08:59:06');

-- ----------------------------
-- Table structure for notifications
-- ----------------------------
DROP TABLE IF EXISTS `notifications`;
CREATE TABLE `notifications`  (
  `NotificationId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Title` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'system',
  `RelatedId` int NULL DEFAULT NULL,
  `IsRead` tinyint(1) NOT NULL DEFAULT 0,
  `IsHandled` tinyint(1) NOT NULL DEFAULT 0,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`NotificationId`) USING BTREE,
  INDEX `idx_user_id`(`UserId` ASC) USING BTREE,
  INDEX `idx_created_at`(`CreatedAt` ASC) USING BTREE,
  CONSTRAINT `notifications_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 6 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of notifications
-- ----------------------------
INSERT INTO `notifications` VALUES (1, 1, '欢迎加入WHU-STU-CHAT', '欢迎加入武汉大学学生互助交流平台！', 'system', NULL, 0, 0, '2025-05-08 11:53:58');
INSERT INTO `notifications` VALUES (2, 3, '系统通知', 'Mike 请求加你为好友', 'system', NULL, 1, 1, '2025-05-08 03:55:58');
INSERT INTO `notifications` VALUES (3, 5, '系统通知', 'Eaxon 请求加你为好友', 'system', NULL, 0, 0, '2025-05-11 08:46:46');
INSERT INTO `notifications` VALUES (4, 3, '系统通知', 'shiro 请求加你为好友', 'system', NULL, 0, 0, '2025-05-11 08:47:43');
INSERT INTO `notifications` VALUES (5, 5, '系统通知', 'Eaxon 请求加你为好友\n验证消息: 我也是蔡老师学生，加个好友！\n', 'friend_request', NULL, 1, 1, '2025-05-11 08:59:56');

-- ----------------------------
-- Table structure for post
-- ----------------------------
DROP TABLE IF EXISTS `post`;
CREATE TABLE `post`  (
  `PostId` int NOT NULL AUTO_INCREMENT,
  `DiscussionId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `AuthorId` int NOT NULL,
  `LikeCount` int NOT NULL DEFAULT 0,
  `CommentCount` int NOT NULL DEFAULT 0,
  `PostType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'normal',
  `IsAnonymous` tinyint(1) NOT NULL DEFAULT 0,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PostId`) USING BTREE,
  INDEX `idx_discussion_id`(`DiscussionId` ASC) USING BTREE,
  INDEX `idx_author_id`(`AuthorId` ASC) USING BTREE,
  INDEX `idx_post_type`(`PostType` ASC) USING BTREE,
  INDEX `idx_update_time`(`UpdateTime` ASC) USING BTREE,
  CONSTRAINT `post_ibfk_1` FOREIGN KEY (`DiscussionId`) REFERENCES `discussion` (`DiscussionId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `post_ibfk_2` FOREIGN KEY (`AuthorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 7 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of post
-- ----------------------------
INSERT INTO `post` VALUES (1, 1, '校园周边美食推荐', '最近发现了几家好吃的餐厅，推荐给大家...', 1, 0, 0, 'normal', 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `post` VALUES (2, 1, '周末活动征集', '有人周末想一起去爬山吗？', 1, 0, 0, 'normal', 0, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `post` VALUES (3, 2, '考研经验分享', '分享一下我备考的一些心得体会...', 1, 1, 1, 'sticky', 0, '2025-05-08 10:55:01', '2025-05-08 11:54:26');
INSERT INTO `post` VALUES (4, 2, '有没有人一起组队参加比赛', '正在找队友参加下个月的编程比赛...', 1, 1, 1, 'normal', 0, '2025-05-08 10:55:01', '2025-05-09 16:51:21');
INSERT INTO `post` VALUES (5, 2, '中午吃什么', '招集饭搭子', 2, 2, 1, 'normal', 1, '2025-05-08 11:17:30', '2025-05-11 16:25:45');
INSERT INTO `post` VALUES (6, 2, 'OS期末怎么复习？求助！', 'OS期末复习求助指导！可联系！', 2, 1, 1, 'normal', 1, '2025-05-09 16:52:06', '2025-05-09 16:52:20');

-- ----------------------------
-- Table structure for postlike
-- ----------------------------
DROP TABLE IF EXISTS `postlike`;
CREATE TABLE `postlike`  (
  `LikeId` int NOT NULL AUTO_INCREMENT,
  `PostId` int NOT NULL,
  `UserId` int NOT NULL,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`LikeId`) USING BTREE,
  UNIQUE INDEX `idx_post_user`(`PostId` ASC, `UserId` ASC) USING BTREE,
  INDEX `idx_user_id`(`UserId` ASC) USING BTREE,
  CONSTRAINT `postlike_ibfk_1` FOREIGN KEY (`PostId`) REFERENCES `post` (`PostId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `postlike_ibfk_2` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 8 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of postlike
-- ----------------------------
INSERT INTO `postlike` VALUES (2, 3, 2, '2025-05-08 11:54:26');
INSERT INTO `postlike` VALUES (3, 5, 2, '2025-05-08 11:54:32');
INSERT INTO `postlike` VALUES (5, 4, 2, '2025-05-09 16:51:21');
INSERT INTO `postlike` VALUES (6, 6, 2, '2025-05-09 16:52:11');
INSERT INTO `postlike` VALUES (7, 5, 3, '2025-05-11 16:25:45');

-- ----------------------------
-- Table structure for privatemessages
-- ----------------------------
DROP TABLE IF EXISTS `privatemessages`;
CREATE TABLE `privatemessages`  (
  `MessageId` int NOT NULL AUTO_INCREMENT,
  `SenderId` int NOT NULL,
  `ReceiverId` int NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
  `MessageType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'text',
  `FileUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileSize` bigint NULL DEFAULT NULL,
  `IsRead` tinyint(1) NULL DEFAULT 0,
  `ReadTime` datetime NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`MessageId`) USING BTREE,
  INDEX `idx_sender_receiver`(`SenderId` ASC, `ReceiverId` ASC) USING BTREE,
  INDEX `idx_receiver_sender`(`ReceiverId` ASC, `SenderId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `privatemessages_ibfk_1` FOREIGN KEY (`SenderId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `privatemessages_ibfk_2` FOREIGN KEY (`ReceiverId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of privatemessages
-- ----------------------------

-- ----------------------------
-- Table structure for roommessages
-- ----------------------------
DROP TABLE IF EXISTS `roommessages`;
CREATE TABLE `roommessages`  (
  `MessageId` int NOT NULL AUTO_INCREMENT,
  `RoomId` int NOT NULL,
  `SenderId` int NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
  `MessageType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT 'text',
  `FileUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FileSize` bigint NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`MessageId`) USING BTREE,
  INDEX `SenderId`(`SenderId` ASC) USING BTREE,
  INDEX `idx_room_id`(`RoomId` ASC) USING BTREE,
  INDEX `idx_create_time`(`CreateTime` ASC) USING BTREE,
  CONSTRAINT `roommessages_ibfk_1` FOREIGN KEY (`RoomId`) REFERENCES `chatrooms` (`RoomId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `roommessages_ibfk_2` FOREIGN KEY (`SenderId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 26 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of roommessages
-- ----------------------------
INSERT INTO `roommessages` VALUES (1, 1, 2, '有人吗？', 'text', NULL, NULL, NULL, '2025-05-08 10:56:41');
INSERT INTO `roommessages` VALUES (2, 1, 3, '你好', 'text', NULL, NULL, NULL, '2025-05-08 10:57:29');
INSERT INTO `roommessages` VALUES (3, 1, 2, 'Hello🥰!', 'text', NULL, NULL, NULL, '2025-05-08 12:06:59');
INSERT INTO `roommessages` VALUES (4, 1, 2, '😇正在上课', 'text', NULL, NULL, NULL, '2025-05-08 12:07:22');
INSERT INTO `roommessages` VALUES (5, 1, 2, '中午吃什么？？', 'text', NULL, NULL, NULL, '2025-05-08 12:12:07');
INSERT INTO `roommessages` VALUES (6, 1, 3, '我都可以，我在计算机学院这边1', 'text', NULL, NULL, NULL, '2025-05-08 12:12:29');
INSERT INTO `roommessages` VALUES (7, 1, 3, '🥲要不就在食堂吧', 'text', NULL, NULL, NULL, '2025-05-08 12:12:37');
INSERT INTO `roommessages` VALUES (8, 1, 3, '我正在学操作系统？有人一起在学习这门课吗？', 'text', NULL, NULL, NULL, '2025-05-09 16:54:23');
INSERT INTO `roommessages` VALUES (9, 1, 2, '我也正在学这门课', 'text', NULL, NULL, NULL, '2025-05-09 16:54:50');
INSERT INTO `roommessages` VALUES (10, 1, 2, '有人吗！！', 'text', NULL, NULL, NULL, '2025-05-09 17:35:17');
INSERT INTO `roommessages` VALUES (11, 1, 3, '有人吗？今天晚上有操作系统实验课', 'text', NULL, NULL, NULL, '2025-05-11 15:43:54');
INSERT INTO `roommessages` VALUES (12, 1, 3, '有人知道在哪个教室上课吗', 'text', NULL, NULL, NULL, '2025-05-11 15:44:01');
INSERT INTO `roommessages` VALUES (13, 1, 3, '我是蔡老师班上的', 'text', NULL, NULL, NULL, '2025-05-11 15:44:05');
INSERT INTO `roommessages` VALUES (14, 1, 2, '我也记不清了', 'text', NULL, NULL, NULL, '2025-05-11 15:45:10');
INSERT INTO `roommessages` VALUES (15, 1, 2, '应该是计算机学院B303', 'text', NULL, NULL, NULL, '2025-05-11 15:45:18');
INSERT INTO `roommessages` VALUES (16, 1, 3, '谢谢', 'text', NULL, NULL, NULL, '2025-05-11 15:47:23');
INSERT INTO `roommessages` VALUES (17, 1, 3, '我再问问老师！', 'text', NULL, NULL, NULL, '2025-05-11 15:47:27');
INSERT INTO `roommessages` VALUES (18, 1, 3, '我也觉得是这个教室', 'text', NULL, NULL, NULL, '2025-05-11 15:47:35');
INSERT INTO `roommessages` VALUES (19, 1, 3, '我准备这会就往过走，感觉第一次课老师会来得早一些哈哈🤣', 'text', NULL, NULL, NULL, '2025-05-11 16:19:34');
INSERT INTO `roommessages` VALUES (20, 1, 2, '有点道理，我也准备往过走了', 'text', NULL, NULL, NULL, '2025-05-11 16:20:05');
INSERT INTO `roommessages` VALUES (21, 1, 2, '就是不知道要带些什么', 'text', NULL, NULL, NULL, '2025-05-11 16:20:14');
INSERT INTO `roommessages` VALUES (22, 1, 2, '可能第一次课需要实验文档？', 'text', NULL, NULL, NULL, '2025-05-11 16:20:26');
INSERT INTO `roommessages` VALUES (23, 1, 5, '有可能，我记得之前学长也说第一次课只用带文档', 'text', NULL, NULL, NULL, '2025-05-11 16:28:06');
INSERT INTO `roommessages` VALUES (24, 1, 5, '因为第一次课没什么困难任务', 'text', NULL, NULL, NULL, '2025-05-11 16:28:20');
INSERT INTO `roommessages` VALUES (25, 1, 5, '主要是模拟环境', 'text', NULL, NULL, NULL, '2025-05-11 16:28:24');

-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `UserId` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Password` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `Phone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `Avatar` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `Signature` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `Status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT 'offline',
  `IsStatusVisible` tinyint(1) NOT NULL DEFAULT 1,
  `LastLoginTime` datetime NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`) USING BTREE,
  UNIQUE INDEX `Username`(`Username` ASC) USING BTREE,
  INDEX `IX_Users_Username`(`Username` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 6 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of users
-- ----------------------------
INSERT INTO `users` VALUES (1, 'testuser', '123456', 'test@whu.edu.cn', NULL, NULL, NULL, 'online', 1, NULL, '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `users` VALUES (2, 'Mike', 'kCFkyK9cHSXDZDyo+OCZjNn9SsW2nwCFJ7D5+lbZp4U=', 'mike666@github.com', '13866660000', NULL, NULL, 'offline', 1, '2025-05-11 16:31:33', '2025-05-08 10:56:08', '2025-05-08 10:56:08');
INSERT INTO `users` VALUES (3, 'Eaxon', 'vIQ63QOS6yY3m4M0v+M/inHd7DSHXlqZw4L06DXCKEc=', 'Eaxon825@aliyun.com', '18191039403', NULL, NULL, 'offline', 1, '2025-05-11 16:32:33', '2025-05-08 10:57:12', '2025-05-08 10:57:12');
INSERT INTO `users` VALUES (4, 'tom', 'v5HfeaDB23bRmBe/ANMGMZgbfRG/uFqCHmUn5iVCyAE=', 'tom123@qq.com', '18290008000', NULL, NULL, 'offline', 1, '2025-05-09 16:55:46', '2025-05-09 16:55:37', '2025-05-09 16:55:37');
INSERT INTO `users` VALUES (5, 'shiro', 'hxr+LMiub6i5KiIoFaMmfz8KbQGfR1IRUoKRyMVhskM=', 'shiro@whu.edu.cn', '13600008888', NULL, NULL, 'offline', 1, '2025-05-11 16:27:34', '2025-05-11 16:27:22', '2025-05-11 16:27:22');

-- ----------------------------
-- Table structure for usersettings
-- ----------------------------
DROP TABLE IF EXISTS `usersettings`;
CREATE TABLE `usersettings`  (
  `SettingId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `SettingKey` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SettingValue` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`SettingId`) USING BTREE,
  UNIQUE INDEX `UserId`(`UserId` ASC, `SettingKey` ASC) USING BTREE,
  CONSTRAINT `usersettings_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of usersettings
-- ----------------------------
INSERT INTO `usersettings` VALUES (1, 1, 'setting_darkMode', 'false', '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `usersettings` VALUES (2, 1, 'setting_messageSound', 'true', '2025-05-08 10:55:01', '2025-05-08 10:55:01');
INSERT INTO `usersettings` VALUES (3, 1, 'setting_showMyOnlineStatus', 'true', '2025-05-08 10:55:01', '2025-05-08 10:55:01');

SET FOREIGN_KEY_CHECKS = 1;

-- -----------------------------------------------------
-- 修复数据库结构和数据一致性问题
-- -----------------------------------------------------

-- 1. 添加 IsPrivate 字段到 ChatGroups 表（如果不存在）
ALTER TABLE chatgroups
ADD COLUMN IF NOT EXISTS IsPrivate TINYINT(1) NOT NULL DEFAULT 0;

-- 2. 修复 comment 表中的外键引用
-- 先删除现有的外键约束
ALTER TABLE comment
DROP FOREIGN KEY comment_ibfk_1;

-- 重新添加外键约束，使其指向正确的 posts 表
ALTER TABLE comment 
ADD CONSTRAINT comment_ibfk_1 FOREIGN KEY (PostId) REFERENCES posts (PostId) ON DELETE CASCADE ON UPDATE RESTRICT;

-- 3. 创建 posts 表（如果不存在）并确保数据一致性
CREATE TABLE IF NOT EXISTS posts (
  `PostId` int NOT NULL AUTO_INCREMENT,
  `DiscussionId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `AuthorId` int NOT NULL,
  `LikeCount` int NOT NULL DEFAULT 0,
  `CommentCount` int NOT NULL DEFAULT 0,
  `PostType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'normal',
  `IsAnonymous` tinyint(1) NOT NULL DEFAULT 0,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PostId`) USING BTREE,
  INDEX `idx_discussion_id`(`DiscussionId` ASC) USING BTREE,
  INDEX `idx_author_id`(`AuthorId` ASC) USING BTREE,
  INDEX `idx_post_type`(`PostType` ASC) USING BTREE,
  INDEX `idx_update_time`(`UpdateTime` ASC) USING BTREE,
  CONSTRAINT `posts_ibfk_1` FOREIGN KEY (`DiscussionId`) REFERENCES `discussions` (`DiscussionId`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `posts_ibfk_2` FOREIGN KEY (`AuthorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- 将 post 表数据迁移到 posts 表（如果存在）
INSERT IGNORE INTO posts (PostId, DiscussionId, Title, Content, AuthorId, LikeCount, CommentCount, PostType, IsAnonymous, CreateTime, UpdateTime)
SELECT p.PostId, p.DiscussionId, p.Title, p.Content, p.AuthorId, p.LikeCount, p.CommentCount, p.PostType, p.IsAnonymous, p.CreateTime, p.UpdateTime
FROM post p
WHERE EXISTS (SELECT 1 FROM post);

-- 4. 创建 discussions 表（如果不存在）并确保数据一致性
CREATE TABLE IF NOT EXISTS discussions (
  `DiscussionId` int NOT NULL AUTO_INCREMENT,
  `Title` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatorId` int NOT NULL,
  `IsHot` tinyint(1) NOT NULL DEFAULT 0,
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`DiscussionId`) USING BTREE,
  INDEX `idx_creator`(`CreatorId` ASC) USING BTREE,
  INDEX `idx_update_time`(`UpdateTime` ASC) USING BTREE,
  CONSTRAINT `discussions_ibfk_1` FOREIGN KEY (`CreatorId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- 将 discussion 表数据迁移到 discussions 表（如果存在）
INSERT IGNORE INTO discussions (DiscussionId, Title, Description, CreatorId, IsHot, CreateTime, UpdateTime)
SELECT d.DiscussionId, d.Title, d.Description, d.CreatorId, d.IsHot, d.CreateTime, d.UpdateTime
FROM discussion d
WHERE EXISTS (SELECT 1 FROM discussion);

-- 5. 修复 postlike 表中的外键引用
-- 先删除现有的外键约束
ALTER TABLE postlike
DROP FOREIGN KEY postlike_ibfk_1;

-- 重新添加外键约束，使其指向正确的 posts 表
ALTER TABLE postlike 
ADD CONSTRAINT postlike_ibfk_1 FOREIGN KEY (PostId) REFERENCES posts (PostId) ON DELETE CASCADE ON UPDATE RESTRICT;

-- 6. 删除冗余表
DROP TABLE IF EXISTS post;
DROP TABLE IF EXISTS discussion;

-- 7. 修复外键约束问题
-- 确保 posts 表的外键正确指向 discussions 表
ALTER TABLE posts
DROP FOREIGN KEY posts_ibfk_1;

ALTER TABLE posts
ADD CONSTRAINT posts_ibfk_1 FOREIGN KEY (DiscussionId) REFERENCES discussions (DiscussionId) ON DELETE CASCADE ON UPDATE RESTRICT;

-- 8. 确保 discussions 表的 CreatorId 外键约束正确
-- 先删除约束（如果存在）
ALTER TABLE discussions
DROP FOREIGN KEY discussions_ibfk_1;

-- 重新添加外键约束
ALTER TABLE discussions
ADD CONSTRAINT discussions_ibfk_1 FOREIGN KEY (CreatorId) REFERENCES users (UserId) ON DELETE RESTRICT ON UPDATE RESTRICT;

-- 9. 添加日期格式修复（转换 2025 年为正确的 2023/2024 年）
-- 注：如果日期有问题，可以使用以下命令来修复，但目前是注释掉的
-- UPDATE users SET CreateTime = DATE_SUB(CreateTime, INTERVAL 2 YEAR), UpdateTime = DATE_SUB(UpdateTime, INTERVAL 2 YEAR);
-- UPDATE posts SET CreateTime = DATE_SUB(CreateTime, INTERVAL 2 YEAR), UpdateTime = DATE_SUB(UpdateTime, INTERVAL 2 YEAR);
-- UPDATE discussions SET CreateTime = DATE_SUB(CreateTime, INTERVAL 2 YEAR), UpdateTime = DATE_SUB(UpdateTime, INTERVAL 2 YEAR);
-- 更多表的日期修复命令可以类似添加

-- 10. 确保 UserController 中的 PUT 端点能够正常工作（HTTP 405 错误问题）
-- 这需要在应用程序代码中检查，但不能在 SQL 中修复

-- 11. 更新用户表以支持资料修改
ALTER TABLE users 
MODIFY COLUMN Email varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
MODIFY COLUMN Phone varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
MODIFY COLUMN Avatar varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
MODIFY COLUMN Signature varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL;

-- 12. 修复 HTTP 405 错误问题（个人资料修改）
-- 这个问题可能与后端代码有关，需要检查 UserController 中是否正确配置了 PUT 端点

-- 结束修复脚本

-- 说明：在执行完本 SQL 脚本后，您需要检查和修复以下代码问题：

-- 1. 前端个人资料更新接口 URL 错误
--    文件路径：frontend/whu-chat-frontend/src/components/UserProfile.vue
--    错误：前端请求使用了错误的URL `/api/user/${userId.value}`
--    修复：将URL修改为 `/api/user/profile`

-- 2. 后端表名大小写不一致问题
--    文件路径：backend/Services/DiscussionService.cs
--    错误：使用表名 'discussion' 而不是 'discussions'
--    修复：将所有 SQL 查询中的 'discussion' 改为 'discussions'

--    文件路径：backend/Services/ChatService.cs  
--    错误：使用表名 'ChatRooms' 和 'RoomMessages' 
--    修复：将所有 SQL 查询中的表名改为小写 'chatrooms' 和 'roommessages'

-- 3. 后端需要迁移数据
--    在前两个问题修复后，执行本 SQL 脚本的修复部分（末尾的 ALTER TABLE 和 CREATE TABLE 语句）

-- 关键提示：执行本脚本后，需要检查以上三个问题是否都已解决，才能确保系统正常运行

-- -----------------------------------------------------
-- 部署说明和表结构说明
-- -----------------------------------------------------

/*
WHU-STU-CHAT 数据库部署指南
=========================

1. 部署顺序和依赖关系
---------------------
该数据库包含多个相互依赖的表，请按照以下顺序部署：
- 先创建users表（用户表，其他表的基础）
- 然后创建discussions/chatrooms/chatgroups等基础表
- 最后创建依赖于上述表的关系表和消息记录表

2. 主要表功能说明
----------------
- users: 用户信息表，存储用户基本信息和登录凭证
- chatrooms: 公共聊天室表，存储系统预设的公共聊天区域
- aichathistory: AI聊天历史记录表，存储用户与AI助手的交流记录
- chatgroups: 聊天群组表，用于群聊功能
- friendships: 好友关系表，维护用户间的好友关系
- notifications: 通知表，用于系统通知和好友请求通知
- discussions/posts/comment: 论坛讨论区相关表

3. 重要配置
----------
- 默认提供了多个测试用户账号，初始密码在users表中
- 默认创建了5个公共聊天室，可以直接使用
- AI聊天功能需要在AIService.cs中配置API密钥

4. 数据恢复
----------
如果遇到外键冲突问题，可以使用本脚本末尾的修复代码部分解决
posts和discussions表之间的依赖关系已在修复脚本中处理

5. 注意事项
----------
- 请确保MySQL版本为8.0或以上
- 确保客户端和服务器的字符集均为utf8mb4
- 建议先删除已有同名数据库，再执行完整脚本创建新库
*/
