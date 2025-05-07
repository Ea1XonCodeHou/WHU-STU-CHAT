-- 创建讨论区表
CREATE TABLE IF NOT EXISTS `Discussions` (
  `DiscussionId` INT NOT NULL AUTO_INCREMENT,
  `Title` VARCHAR(100) NOT NULL,
  `Description` TEXT NOT NULL,
  `CreatorId` INT NOT NULL,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  `PostCount` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`DiscussionId`),
  INDEX `idx_creator_id` (`CreatorId`),
  FOREIGN KEY (`CreatorId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 创建帖子表
CREATE TABLE IF NOT EXISTS `Posts` (
  `PostId` INT NOT NULL AUTO_INCREMENT,
  `DiscussionId` INT NOT NULL,
  `Title` VARCHAR(200) NOT NULL,
  `Content` TEXT NOT NULL,
  `AuthorId` INT NOT NULL,
  `IsAnonymous` TINYINT(1) NOT NULL DEFAULT 0,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  `ViewCount` INT NOT NULL DEFAULT 0,
  `LikeCount` INT NOT NULL DEFAULT 0,
  `CommentCount` INT NOT NULL DEFAULT 0,
  `PostType` VARCHAR(20) NOT NULL DEFAULT 'normal',
  PRIMARY KEY (`PostId`),
  INDEX `idx_discussion_id` (`DiscussionId`),
  INDEX `idx_author_id` (`AuthorId`),
  FOREIGN KEY (`DiscussionId`) REFERENCES `Discussions`(`DiscussionId`) ON DELETE CASCADE,
  FOREIGN KEY (`AuthorId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 创建评论表
CREATE TABLE IF NOT EXISTS `Comments` (
  `CommentId` INT NOT NULL AUTO_INCREMENT,
  `PostId` INT NOT NULL,
  `Content` TEXT NOT NULL,
  `AuthorId` INT NOT NULL,
  `IsAnonymous` TINYINT(1) NOT NULL DEFAULT 0,
  `CreateTime` DATETIME NOT NULL,
  `UpdateTime` DATETIME NOT NULL,
  `LikeCount` INT NOT NULL DEFAULT 0,
  `ParentId` INT NULL,
  PRIMARY KEY (`CommentId`),
  INDEX `idx_post_id` (`PostId`),
  INDEX `idx_author_id` (`AuthorId`),
  INDEX `idx_parent_id` (`ParentId`),
  FOREIGN KEY (`PostId`) REFERENCES `Posts`(`PostId`) ON DELETE CASCADE,
  FOREIGN KEY (`AuthorId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE,
  FOREIGN KEY (`ParentId`) REFERENCES `Comments`(`CommentId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci; 