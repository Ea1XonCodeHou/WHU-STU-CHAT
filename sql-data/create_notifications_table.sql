-- 创建通知表
CREATE TABLE IF NOT EXISTS `Notifications` (
  `NotificationId` INT NOT NULL AUTO_INCREMENT,
  `UserId` INT NOT NULL,
  `Content` TEXT NOT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsHandled` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`NotificationId`),
  INDEX `idx_user_id` (`UserId`),
  FOREIGN KEY (`UserId`) REFERENCES `Users`(`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 添加测试数据
INSERT INTO `Notifications` (`UserId`, `Content`, `CreatedAt`, `IsHandled`)
VALUES
(1, 'System Notification: Welcome to WHU-STU-CHAT', NOW() - INTERVAL 1 DAY, 0);

-- 确保通知ID从1开始
ALTER TABLE `Notifications` AUTO_INCREMENT = 1; 