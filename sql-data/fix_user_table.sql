-- 修复Users表中的Status字段问题

-- 检查Status字段是否存在
SET @column_exists = 0;
SELECT COUNT(*) INTO @column_exists 
FROM information_schema.COLUMNS 
WHERE TABLE_SCHEMA = 'whu-chat' 
AND TABLE_NAME = 'Users' 
AND COLUMN_NAME = 'Status';

-- 如果字段不存在，添加它
SET @query = IF(@column_exists = 0, 
                'ALTER TABLE Users ADD COLUMN Status VARCHAR(20) DEFAULT "offline" AFTER Signature;', 
                'SELECT "Status column already exists";');
PREPARE stmt FROM @query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 更新所有现有用户的状态为离线
UPDATE Users SET Status = 'offline' WHERE Status IS NULL;

-- 创建测试用户（如果用户5和用户6不存在）
INSERT IGNORE INTO Users (UserId, Username, Password, Email, Status, CreateTime, UpdateTime)
VALUES 
(5, '东海帝皇', '123456', 'test5@whu.edu.cn', 'offline', NOW(), NOW()),
(6, '好歌剧', '123456', 'test6@whu.edu.cn', 'offline', NOW(), NOW());

-- 更新测试用户的个性签名
UPDATE Users SET Signature = '大事发生' WHERE UserId = 5;
UPDATE Users SET Signature = '阿斯蒂芬' WHERE UserId = 6; 