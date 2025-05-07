-- 创建私聊消息表
CREATE TABLE IF NOT EXISTS `private_messages` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `sender_id` INT NOT NULL,
  `receiver_id` INT NOT NULL,
  `content` TEXT NULL,
  `message_type` VARCHAR(20) NOT NULL DEFAULT 'text',
  `file_url` VARCHAR(255) NULL,
  `file_name` VARCHAR(255) NULL,
  `file_size` BIGINT NULL,
  `send_time` DATETIME NOT NULL,
  `is_read` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `idx_sender_receiver` (`sender_id`, `receiver_id`),
  INDEX `idx_receiver_sender` (`receiver_id`, `sender_id`),
  INDEX `idx_send_time` (`send_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 添加测试数据（使用存在的用户ID）
INSERT INTO `private_messages` (`sender_id`, `receiver_id`, `content`, `message_type`, `send_time`, `is_read`)
VALUES
(1, 2, '你好，这是一条测试消息', 'text', NOW() - INTERVAL 2 HOUR, 1),
(2, 1, '收到，这是回复', 'text', NOW() - INTERVAL 1 HOUR, 1),
(1, 2, '我们来测试一下私聊功能', 'text', NOW() - INTERVAL 30 MINUTE, 1),
(2, 1, '好的，私聊功能测试中', 'text', NOW() - INTERVAL 25 MINUTE, 1);

-- 确保消息ID从1开始
ALTER TABLE `private_messages` AUTO_INCREMENT = 1; 