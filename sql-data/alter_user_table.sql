-- 修改Users表，添加Status字段
ALTER TABLE Users
ADD COLUMN IF NOT EXISTS Status VARCHAR(20) DEFAULT 'offline' AFTER Signature;

-- 更新现有用户的状态为离线
UPDATE Users SET Status = 'offline' WHERE Status IS NULL; 