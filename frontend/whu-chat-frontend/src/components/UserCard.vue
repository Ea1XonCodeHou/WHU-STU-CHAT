<template>
  <div class="user-card-overlay" v-if="visible" @click="closeCard">
    <div class="user-card" @click.stop>
      <div class="user-card-header">
        <h3>用户名片</h3>
        <button class="close-button" @click="closeCard">×</button>
      </div>
      <div class="user-card-content">
        <div class="user-avatar">
          <img v-if="user.avatar" :src="user.avatar" :alt="user.username">
          <div v-else class="avatar-placeholder">{{ user.username ? user.username.charAt(0).toUpperCase() : '?' }}</div>
          <div class="status-indicator" :class="user.status || 'offline'"></div>
        </div>
        
        <div class="user-info">
          <div class="username">{{ user.username }}</div>
          <div class="status">{{ user.status === 'online' ? '在线' : '离线' }}</div>
          <div class="signature" v-if="user.signature">{{ user.signature }}</div>
          <div class="user-stats">
            <div class="stat-item">
              <span class="stat-label">用户ID</span>
              <span class="stat-value">{{ user.id }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">最后活跃</span>
              <span class="stat-value">{{ formatDate(user.lastActive) }}</span>
            </div>
          </div>
        </div>
        
        <div class="card-actions">
          <!-- 已经是好友 -->
          <div v-if="isFriendWithUser" class="friend-status">
            <i class="fa-solid fa-check"></i> 已是好友
          </div>
          
          <!-- 待处理状态 -->
          <div v-else-if="friendshipStatus === 'pending'" class="friend-status pending">
            <i class="fa-solid fa-clock"></i> 好友请求待处理
          </div>
          
          <!-- 被对方拒绝 -->
          <div v-else-if="friendshipStatus === 'rejected'" class="friend-status rejected">
            <i class="fa-solid fa-ban"></i> 请求已被拒绝
            <button class="action-button add-friend" @click="showAddFriendDialog = true">
              <i class="fa-solid fa-user-plus"></i> 重新发送请求
            </button>
          </div>
          
          <!-- 可以添加好友 -->
          <div v-else class="action-buttons">
            <button class="action-button add-friend" @click="showAddFriendDialog = true">
              <i class="fa-solid fa-user-plus"></i> 添加好友
            </button>
          </div>
          
          <button class="action-button special-attention" 
                  :class="{ active: isSpecialAttention }"
                  @click="toggleSpecialAttention">
            <i class="fa-solid" :class="isSpecialAttention ? 'fa-star' : 'fa-star-half-alt'"></i>
            {{ isSpecialAttention ? '取消特别关注' : '特别关注' }}
          </button>
        </div>
      </div>
      
      <!-- 添加好友弹窗 -->
      <div class="add-friend-dialog" v-if="showAddFriendDialog">
        <div class="dialog-header">
          <h4>添加好友</h4>
        </div>
        <div class="dialog-content">
          <div class="form-group">
            <label for="message">验证消息</label>
            <textarea 
              id="message" 
              v-model="friendRequestMessage" 
              placeholder="请输入验证消息..." 
              rows="3"
            ></textarea>
          </div>
        </div>
        <div class="dialog-footer">
          <button class="cancel-button" @click="showAddFriendDialog = false">取消</button>
          <button class="confirm-button" @click="sendFriendRequest">发送请求</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

export default {
  name: 'UserCard',
  props: {
    visible: {
      type: Boolean,
      default: false
    },
    userId: {
      type: Number,
      required: true
    },
    isFriend: {
      type: Boolean,
      default: false
    }
  },
  emits: ['close', 'friend-request-sent'],
  setup(props, { emit }) {
    const user = ref({});
    const showAddFriendDialog = ref(false);
    const friendRequestMessage = ref('');
    const isSpecialAttention = ref(false);
    const currentUserId = parseInt(localStorage.getItem('userId') || '0');
    const currentUsername = localStorage.getItem('username') || '';
    const isFriendWithUser = ref(props.isFriend);
    const friendshipStatus = ref('none');
    
    // 加载用户详细信息
    const loadUserInfo = async () => {
      try {
        const response = await axios.get(`/api/user/${props.userId}`);
        if (response.data && response.data.code === 200) {
          user.value = response.data.data;
          
          // 查询好友关系状态
          await checkFriendshipStatus();
        }
      } catch (error) {
        console.error('获取用户信息失败:', error);
      }
    };
    
    // 检查好友关系状态
    const checkFriendshipStatus = async () => {
      try {
        const response = await axios.get(`/api/notification/friendship/${currentUserId}/${props.userId}`);
        if (response.data && response.data.status) {
          friendshipStatus.value = response.data.status;
          isFriendWithUser.value = response.data.status === 'accepted';
        }
      } catch (error) {
        console.error('获取好友关系状态失败:', error);
      }
    };
    
    // 格式化日期
    const formatDate = (dateString) => {
      if (!dateString) return '未知';
      
      const date = new Date(dateString);
      const now = new Date();
      const diff = now.getTime() - date.getTime();
      
      // 如果小于24小时，显示x小时前
      if (diff < 24 * 60 * 60 * 1000) {
        const hours = Math.floor(diff / (60 * 60 * 1000));
        return hours > 0 ? `${hours}小时前` : '刚刚';
      }
      
      // 小于7天，显示x天前
      if (diff < 7 * 24 * 60 * 60 * 1000) {
        const days = Math.floor(diff / (24 * 60 * 60 * 1000));
        return `${days}天前`;
      }
      
      // 其他情况显示具体日期
      return date.toLocaleDateString('zh-CN');
    };
    
    // 关闭名片
    const closeCard = () => {
      emit('close');
    };
    
    // 切换特别关注状态
    const toggleSpecialAttention = () => {
      isSpecialAttention.value = !isSpecialAttention.value;
      
      // TODO: 实际项目中可以发送请求到后端保存状态
      const message = isSpecialAttention.value ? 
        `已将用户 ${user.value.username} 设为特别关注` : 
        `已取消用户 ${user.value.username} 的特别关注`;
      
      alert(message);
    };
    
    // 发送好友请求
    const sendFriendRequest = async () => {
      try {
        // 如果已经发送过请求，显示提示
        if (friendshipStatus.value === 'pending') {
          alert('已经发送过好友请求，请等待对方回应');
          showAddFriendDialog.value = false;
          return;
        }
        
        const response = await axios.post('/api/notification/friend-request', {
          targetUsername: user.value.username,
          requesterUsername: currentUsername,
          message: friendRequestMessage.value || '我想加你为好友'
        });
        
        alert('好友请求已发送');
        showAddFriendDialog.value = false;
        friendRequestMessage.value = '';
        friendshipStatus.value = 'pending'; // 更新状态
        emit('friend-request-sent');
      } catch (error) {
        console.error('发送好友请求失败:', error);
        alert('发送好友请求失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    onMounted(() => {
      loadUserInfo();
    });
    
    return {
      user,
      showAddFriendDialog,
      friendRequestMessage,
      isSpecialAttention,
      formatDate,
      closeCard,
      toggleSpecialAttention,
      sendFriendRequest,
      isFriendWithUser,
      friendshipStatus
    };
  }
};
</script>

<style scoped>
.user-card-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.user-card {
  width: 400px;
  max-width: 90vw;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  overflow: hidden;
  position: relative;
}

.user-card-header {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  padding: 15px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.user-card-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.close-button {
  background: none;
  border: none;
  color: white;
  font-size: 24px;
  cursor: pointer;
  line-height: 1;
}

.user-card-content {
  padding: 20px;
}

.user-avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  margin: 0 auto 15px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background-color: #f0f0f0;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  font-size: 30px;
  font-weight: bold;
}

.status-indicator {
  position: absolute;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background-color: #8A8A8E;
  border: 2px solid white;
  bottom: 3px;
  right: 3px;
}

.status-indicator.online {
  background-color: #4CD964;
}

.user-info {
  text-align: center;
  margin-bottom: 20px;
}

.username {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 5px;
}

.status {
  font-size: 14px;
  color: #666;
  margin-bottom: 5px;
}

.signature {
  font-size: 14px;
  color: #666;
  margin-bottom: 15px;
  font-style: italic;
}

.user-stats {
  display: flex;
  justify-content: center;
  gap: 20px;
  margin-top: 15px;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.stat-label {
  font-size: 12px;
  color: #999;
  margin-bottom: 2px;
}

.stat-value {
  font-size: 14px;
  color: #333;
  font-weight: 600;
}

.card-actions {
  display: flex;
  justify-content: center;
  gap: 10px;
  margin-top: 20px;
}

.friend-status {
  padding: 10px 15px;
  border-radius: 8px;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  font-size: 14px;
  background-color: #f5f5f5;
  color: #333;
}

.friend-status.pending {
  background-color: #fff3cd;
  color: #856404;
}

.friend-status.rejected {
  background-color: #f8d7da;
  color: #721c24;
  flex-direction: column;
  padding-bottom: 5px;
}

.friend-status.rejected button {
  margin-top: 8px;
  padding: 6px 12px;
  font-size: 12px;
}

.friend-status i {
  font-size: 16px;
}

.action-buttons {
  display: flex;
  gap: 10px;
}

.action-button {
  flex: 1;
  padding: 10px 15px;
  border-radius: 8px;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.action-button:hover:not(:disabled) {
  transform: translateY(-2px);
}

.action-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.action-button.add-friend {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.action-button.special-attention {
  background-color: #f5f5f5;
  color: #333;
}

.action-button i {
  font-size: 14px;
}

/* 添加好友弹窗 */
.add-friend-dialog {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: white;
  z-index: 10;
  display: flex;
  flex-direction: column;
}

.dialog-header {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  padding: 15px 20px;
}

.dialog-header h4 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
}

.dialog-content {
  padding: 20px;
  flex: 1;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 8px;
  resize: none;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group textarea:focus {
  border-color: #4776E6;
  outline: none;
}

.dialog-footer {
  padding: 15px 20px;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  border-top: 1px solid #eee;
}

.cancel-button, .confirm-button {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-button {
  background-color: #f5f5f5;
  color: #333;
  border: none;
}

.confirm-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
}

.cancel-button:hover {
  background-color: #eee;
}

.confirm-button:hover {
  box-shadow: 0 2px 8px rgba(71, 118, 230, 0.3);
}
</style> 