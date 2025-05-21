<template>
  <div class="chat-room-container">
    <!-- 头部信息 -->
    <header class="chat-header">
      <div class="room-info">
        <h1>{{ currentGroup ? currentGroup.groupName : '群组聊天' }}</h1>
        <div class="room-status">
          <span class="status-indicator" :class="{ 'connected': isConnected }"></span>
          <span class="status-text">{{ connectionStatus }}</span>
          <span class="online-count">成员: {{ currentGroup ? currentGroup.memberCount : 0 }}</span>
        </div>
      </div>
      <div class="user-info">
        <span class="username">{{ username }}</span>
        <div class="avatar" v-if="userAvatar">
          <img :src="userAvatar" alt="用户头像" />
        </div>
        <div class="avatar default-avatar" v-else>
          {{ username?.charAt(0)?.toUpperCase() || '?' }}
        </div>
        <button class="leave-button" @click="leaveRoom">退出聊天室</button>
      </div>
    </header>

    <!-- 主内容区 -->
    <main class="chat-main">
      <!-- 群组列表侧边栏 -->
      <div class="sidebar">
        <div class="sidebar-content">
          <!-- 群组列表部分 -->
          <div class="sidebar-section groups-section">
            <div class="sidebar-header">
              <h2>我的群组</h2>
              <div class="search-box">
                <input 
                  type="text" 
                  v-model="groupSearch" 
                  placeholder="搜索群组..." 
                  @input="handleGroupSearch"
                >
              </div>
            </div>
            <div class="groups-list">
              <div 
                v-for="group in groups" 
                :key="group.groupId" 
                class="group-item"
                :class="{ 'active': currentGroup && currentGroup.groupId === group.groupId }"
                @click="selectGroup(group)"
              >
                <div class="group-avatar">
                  <span class="default-avatar">{{ group?.groupName?.charAt(0)?.toUpperCase() || '?' }}</span>
                </div>
                <div class="group-details">
                  <div class="group-name">{{ group.groupName }}</div>
                  <div class="group-members">{{ group.memberCount }} 名成员</div>
                </div>
              </div>
              
              <div v-if="groups.length === 0" class="empty-groups">
                <i class="groups-icon"></i>
                <p>暂无群组</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 聊天消息区 -->
      <div class="messages-container" ref="messagesContainer">
        <div class="messages-wrapper">
          <div v-if="!currentGroup" class="empty-state">
            <i class="chat-icon"></i>
            <p>请选择一个群组开始聊天</p>
          </div>
          <div v-else-if="messages.length === 0" class="empty-state">
            <i class="chat-icon"></i>
            <p>还没有任何消息，开始聊天吧！</p>
          </div>

          <div v-for="(message, index) in messages" :key="message.messageId || index" 
               class="message-item" 
               :class="getMessageClass(message)">
            
            <!-- 日期分隔线 -->
            <div v-if="shouldShowDateSeparator(index)" class="date-separator">
              <span>{{ formatDate(message.sendTime) }}</span>
            </div>
            
            <!-- 系统消息 -->
            <div v-if="message.messageType === 'system'" class="system-message">
              <div class="system-message-content">
                <i class="system-icon"></i>
                <span>{{ message.content }}</span>
              </div>
              <div class="message-time">{{ formatTime(message.sendTime) }}</div>
            </div>
            
            <!-- 用户消息 -->
            <div v-else class="user-message" :class="{'self-message': message.senderId === userId}">
              <!-- 头像 -->
              <div class="message-avatar" v-if="message.senderId !== userId" @click.stop="showUserCard(message.senderId)">
                <div class="avatar" v-if="message.senderAvatar">
                  <img :src="message.senderAvatar" alt="用户头像" @error="handleImageError" />
                </div>
                <div class="avatar default-avatar" v-else>
                  {{ message?.senderName?.charAt(0)?.toUpperCase() || '?' }}
                </div>
              </div>
              
              <!-- 消息内容 -->
              <div class="message-content">
                <div class="message-info">
                  <span class="message-sender" v-if="message.senderId !== userId" @click.stop="showUserCard(message.senderId)">{{ message.senderName }}</span>
                  <span class="message-time">{{ formatTime(message.sendTime) }}</span>
                </div>
                
                <!-- 文本消息 -->
                <div v-if="message.messageType === 'text'" class="message-text" :class="{'unknown-user': message.senderName === '未知用户'}">
                  {{ message.content }}
                </div>
                
                <!-- 图片消息 -->
                <div v-else-if="message.messageType === 'image'" class="message-image">
                  <img 
                    :src="getFullImageUrl(message.fileUrl)" 
                    alt="图片消息" 
                    @click="previewImage(message.fileUrl)" 
                    @error="handleImageError"
                    loading="lazy" />
                  <div class="image-info">{{ message.fileName }} ({{ formatFileSize(message.fileSize) }})</div>
                </div>
                
                <!-- 文件消息 -->
                <div v-else-if="message.messageType === 'file'" class="message-file" @click="downloadFile(message.fileUrl, message.fileName)">
                  <div class="file-icon"></div>
                  <div class="file-info">
                    <div class="file-name">{{ message.fileName }}</div>
                    <div class="file-size">{{ formatFileSize(message.fileSize) }}</div>
                  </div>
                  <div class="download-icon"></div>
                </div>
                
                <!-- 表情消息 -->
                <div v-else-if="message.messageType === 'emoji'" class="message-emoji">
                  {{ message.content }}
                </div>
              </div>
              
              <!-- 右侧头像(自己的消息) -->
              <div class="self-avatar" v-if="message.senderId === userId">
                <div class="avatar" v-if="userAvatar">
                  <img :src="userAvatar" alt="用户头像" @error="handleImageError" />
                </div>
                <div class="avatar default-avatar" v-else>
                  {{ username?.charAt(0)?.toUpperCase() || '?' }}
                </div>
              </div>
            </div>
          </div>
          
          <!-- 新消息提示 -->
          <div v-if="hasNewMessage && !isAtBottom" class="new-message-indicator" @click="scrollToBottom">
            <i class="arrow-down-icon"></i>
            <span>有新消息</span>
          </div>
        </div>
      </div>
    </main>

    <!-- 底部输入区 -->
    <footer class="chat-footer" v-if="currentGroup">
      <!-- 工具栏 -->
      <div class="toolbar">
        <div class="tool-button emoji-button" @click="toggleEmojiPanel">
          <i class="emoji-icon"></i>
        </div>
        <div class="tool-button image-button" @click="triggerImageUpload">
          <i class="image-icon"></i>
          <input 
            type="file" 
            ref="imageInput" 
            accept="image/*" 
            style="display: none" 
            @change="handleImageUpload"
          >
        </div>
      </div>
      
      <!-- 输入框 -->
      <div class="input-container">
        <!-- 表情面板 -->
        <div v-if="showEmojiPanel" class="emoji-panel">
          <div v-for="emoji in emojis" :key="emoji" class="emoji-item" @click="insertEmoji(emoji)">
            {{ emoji }}
          </div>
        </div>
        
        <textarea 
          class="message-input" 
          v-model="messageText" 
          placeholder="输入消息..." 
          @keydown.enter.prevent="sendMessage"
          ref="messageInput"></textarea>
      </div>
      
      <!-- 发送按钮 -->
      <button class="send-button" @click="sendMessage" :disabled="!isConnected || !messageText.trim()">
        <i class="send-icon"></i>
        <span>发送</span>
      </button>
    </footer>

    <!-- 图片预览弹窗 -->
    <div v-if="previewImageUrl" class="image-preview-modal" @click="closeImagePreview">
      <div class="image-preview-content">
        <img 
          :src="getFullImageUrl(previewImageUrl)" 
          alt="图片预览" 
          @error="handleImageError" />
        <button class="close-preview" @click.stop="closeImagePreview">×</button>
      </div>
    </div>

    <!-- 用户名片弹窗 -->
    <user-card 
      v-if="showingUserCard" 
      :visible="showingUserCard"
      :user-id="selectedUserId"
      :is-friend="isUserFriend(selectedUserId)"
      @close="closeUserCard"
      @friend-request-sent="handleFriendRequestSent"
    />

    <!-- 提示信息 -->
    <div v-if="notification.show" class="notification" :class="notification.type">
      {{ notification.message }}
    </div>
    
    <!-- 创建群组模态框 -->
    <div class="modal" v-if="showCreateGroupModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>创建新群组</h3>
          <button @click="showCreateGroupModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>群组名称</label>
            <input v-model="newGroup.groupName" type="text" placeholder="输入群组名称">
          </div>
          <div class="form-group">
            <label>群组描述</label>
            <textarea v-model="newGroup.description" placeholder="输入群组描述"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showCreateGroupModal = false" class="cancel-btn">取消</button>
          <button @click="createGroup" class="confirm-btn">创建</button>
        </div>
      </div>
    </div>

    <!-- 添加用户模态框 -->
    <div class="modal" v-if="showAddUserModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>添加成员</h3>
          <button @click="showAddUserModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>用户ID</label>
            <input v-model="newMemberId" type="number" placeholder="输入要添加的用户ID">
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showAddUserModal = false" class="cancel-btn">取消</button>
          <button @click="addUserToGroup" class="confirm-btn">添加</button>
        </div>
      </div>
    </div>

    <!-- 移除用户模态框 -->
    <div class="modal" v-if="showRemoveUserModal">
      <div class="modal-content">
        <div class="modal-header">
          <h3>移除成员</h3>
          <button @click="showRemoveUserModal = false" class="close-btn">
            <span class="material-icon">close</span>
          </button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>用户ID</label>
            <input v-model="removeMemberId" type="number" placeholder="输入要移除的用户ID">
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showRemoveUserModal = false" class="cancel-btn">取消</button>
          <button @click="removeUserFromGroup" class="confirm-btn">移除</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, computed, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import UserCard from '@/components/UserCard.vue';

export default {
  name: 'GroupChat',
  components: {
    UserCard
  },
  setup() {
    const route = useRoute();
    const router = useRouter();
    
    // 用户信息
    const userId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const username = ref(localStorage.getItem('username') || '访客');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    
    // 群组信息
    const groups = ref([]);
    const currentGroup = ref(null);
    const groupSearch = ref('');
    
    // 连接状态
    const isConnected = ref(false);
    const connectionStatus = ref('正在连接...');
    const connection = ref(null);
    
    // 消息相关
    const messages = ref([]);
    const messageText = ref('');
    const loadingHistory = ref(true);
    const messagesContainer = ref(null);
    const messageInput = ref(null);
    const hasNewMessage = ref(false);
    const isAtBottom = ref(true);
    
    // 表情相关
    const showEmojiPanel = ref(false);
    const emojis = ref(['😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '🥲', '☺️', '😊', '😇', 
                      '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛', 
                      '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🥸', '🤩', '🥳', '😏', '😒']);
    
    // 文件上传相关
    const imageInput = ref(null);
    const fileInput = ref(null);
    
    // 图片预览
    const previewImageUrl = ref(null);
    
    // 通知提示
    const notification = ref({
      show: false,
      message: '',
      type: 'info',
      timeout: null
    });
    
    // 群组管理相关
    const showCreateGroupModal = ref(false);
    const showAddUserModal = ref(false);
    const showRemoveUserModal = ref(false);
    const newGroup = ref({
        groupName: '',
      description: ''
    });
    const newMemberId = ref(null);
    const removeMemberId = ref(null);

    // 用户名片相关
    const showingUserCard = ref(false);
    const selectedUserId = ref(null);
    const friendsList = ref([]);

    // 加载好友列表
    const loadFriendsList = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/group/user/${userId.value}/private`);
        if (response.data && response.data.code === 200) {
          friendsList.value = response.data.data.map(group => {
            // 查找对方用户ID（好友ID）
            const otherMember = group.members?.find(m => m.id !== userId.value);
            return otherMember ? otherMember.id : null;
          }).filter(id => id !== null);
        }
      } catch (error) {
        console.error('获取好友列表失败:', error);
      }
    };
    
    // 判断用户是否是好友
    const isUserFriend = (userId) => {
      return friendsList.value.includes(userId);
    };
    
    // 显示用户名片
    const showUserCard = (userId) => {
      // 不要显示自己的名片
      if (userId === parseInt(localStorage.getItem('userId'))) {
        return;
      }
      
      selectedUserId.value = userId;
      showingUserCard.value = true;
    };
    
    // 关闭用户名片
    const closeUserCard = () => {
      showingUserCard.value = false;
      selectedUserId.value = null;
    };
    
    // 处理好友请求发送后的回调
    const handleFriendRequestSent = () => {
      closeUserCard();
      showNotification('好友请求已发送', 'success');
    };

    // 创建SignalR连接
    const createConnection = () => {
      const apiBaseUrl = 'http://localhost:5067';
      
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${apiBaseUrl}/groupChatHub`)
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();
      
      window.apiBaseUrl = apiBaseUrl;
      
      connection.value.onreconnecting(() => {
        isConnected.value = false;
        connectionStatus.value = '正在重新连接...';
        showNotification('连接断开，正在尝试重连...', 'warning');
      });
      
      connection.value.onreconnected(() => {
        isConnected.value = true;
        connectionStatus.value = '已连接';
        showNotification('已重新连接到聊天室', 'success');
        
        if (currentGroup.value) {
          joinGroup(currentGroup.value.groupId);
        }
      });
      
      connection.value.onclose(() => {
        isConnected.value = false;
        connectionStatus.value = '连接已关闭';
        showNotification('连接已关闭', 'error');
      });
      
      connection.value.on('Error', (errorMessage) => {
        console.error('SignalR错误:', errorMessage);
        if (errorMessage.includes('用户已在群组中')) {
          showNotification('已成功加入群组', 'success');
        } else {
          showNotification(errorMessage, 'error');
        }
      });
      
      registerSignalRHandlers();
      startConnection();
    };
    
    // 获取用户信息
    const getUserInfo = async (userId) => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/User/${userId}`);
        if (response.data && response.data.code === 200) {
          const userInfo = response.data.data;
          // 处理头像URL
          if (userInfo.avatar) {
            userInfo.avatar = userInfo.avatar.startsWith('http') 
              ? userInfo.avatar 
              : `${window.apiBaseUrl}${userInfo.avatar}`;
          }
          return userInfo;
        }
        return null;
      } catch (error) {
        console.error('获取用户信息失败:', error);
        return null;
      }
    };
    
    // 注册SignalR处理函数
    const registerSignalRHandlers = () => {
      connection.value.on('ReceiveMessage', async (message) => {
        console.log('收到新消息:', message);
        
        // 获取发送者信息
        const senderInfo = await getUserInfo(message.senderId);
        
        // 检查是否是图片消息
        const isImageMessage = message.content && (
          message.content.startsWith('http') && 
          (message.content.endsWith('.jpg') || message.content.endsWith('.jpeg') || message.content.endsWith('.png') || message.content.endsWith('.gif') || message.content.endsWith('.bmp') || message.content.endsWith('.webp'))
        );
        
        // 将后端消息格式转换为前端需要的格式
        const formattedMessage = {
          messageId: message.messageId,
          content: message.content,
          sendTime: message.createTime,
          senderId: message.senderId,
          senderName: senderInfo?.username || '未知用户',
          senderAvatar: senderInfo?.avatar || null,
          groupId: message.groupId,
          messageType: isImageMessage ? 'image' : (message.messageType || 'text'),
          fileUrl: isImageMessage ? getFullImageUrl(message.content) : getFullImageUrl(message.fileUrl),
          fileName: message.fileName || (isImageMessage ? message.content.split('/').pop() : null),
          fileSize: message.fileSize || 0
        };
        messages.value.push(formattedMessage);
        
        if (isAtBottom.value) {
          nextTick(() => scrollToBottom());
        } else {
          hasNewMessage.value = true;
        }
        
        if (message.senderId !== userId.value && message.messageType !== 'system') {
          showNotification(`${formattedMessage.senderName}: ${message.content}`, 'info');
        }
      });
      
      connection.value.on('ReceiveHistoryMessages', async (historyMessages) => {
        console.log('收到历史消息:', historyMessages);
        loadingHistory.value = false;
        
        if (historyMessages && historyMessages.length > 0) {
          // 获取所有发送者的用户信息
          const senderIds = [...new Set(historyMessages.map(msg => msg.senderId))];
          const userInfos = await Promise.all(
            senderIds.map(async (id) => {
              const userInfo = await getUserInfo(id);
              return { 
                id, 
                username: userInfo?.username || '未知用户',
                avatar: userInfo?.avatar || null
              };
            })
          );
          
          // 将后端消息格式转换为前端需要的格式
          const formattedMessages = historyMessages.map(msg => {
            const senderInfo = userInfos.find(u => u.id === msg.senderId);
            
            // 检查是否是图片消息
            const isImageMessage = msg.content && (
              msg.content.startsWith('http') && 
              (msg.content.endsWith('.jpg') || msg.content.endsWith('.jpeg') || msg.content.endsWith('.png') || msg.content.endsWith('.gif') || msg.content.endsWith('.bmp') || msg.content.endsWith('.webp'))
            );
            
            const formattedMsg = {
              messageId: msg.messageId,
              content: msg.content,
              sendTime: msg.createTime,
              senderId: msg.senderId,
              senderName: senderInfo?.username || '未知用户',
              senderAvatar: senderInfo?.avatar || null,
              groupId: msg.groupId,
              messageType: isImageMessage ? 'image' : (msg.messageType || 'text'),
              fileUrl: isImageMessage ? getFullImageUrl(msg.content) : getFullImageUrl(msg.fileUrl),
              fileName: msg.fileName || (isImageMessage ? msg.content.split('/').pop() : null),
              fileSize: msg.fileSize || 0
            };
            return formattedMsg;
          }).sort((a, b) => new Date(a.sendTime) - new Date(b.sendTime));
          messages.value = formattedMessages;
          
          nextTick(() => scrollToBottom());
        }
      });
      
      connection.value.on('UpdateGroupMembers', (members) => {
        if (currentGroup.value) {
          currentGroup.value.memberCount = members.length;
        }
      });
    };
    
    // 启动连接
    const startConnection = async () => {
      try {
        connectionStatus.value = '正在连接...';
        await connection.value.start();
        isConnected.value = true;
        connectionStatus.value = '已连接';
        console.log('SignalR连接已建立');
        
    // 加载用户群组列表
        loadUserGroups();
      } catch (error) {
        console.error('连接SignalR失败:', error);
        connectionStatus.value = '连接失败';
        isConnected.value = false;
        
        setTimeout(startConnection, 5000);
      }
    };
    
    // 加载用户群组列表
    const loadUserGroups = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/Group/user/${userId.value}`);
        if (response.data && response.data.code === 200) {
          groups.value = response.data.data;
        } else {
          throw new Error(response.data?.msg || '获取群组列表失败');
        }
      } catch (error) {
        console.error('加载群组列表失败:', error);
        showNotification('加载群组列表失败: ' + error.message, 'error');
      }
    };
    
    // 选择群组
    const selectGroup = async (group) => {
      currentGroup.value = group;
      messages.value = [];
      loadingHistory.value = true;
      
      try {
        // 加入群组
        await joinGroup(group.groupId);
        
        // 获取历史消息
        const response = await axios.get(`${window.apiBaseUrl}/api/Group/${group.groupId}/messages?count=50`);
        if (response.data && response.data.code === 200) {
          console.log('历史消息原始数据:', response.data.data);
          
          // 获取所有发送者的用户信息
          const senderIds = [...new Set(response.data.data.map(msg => msg.senderId))];
          const userInfos = await Promise.all(
            senderIds.map(async (id) => {
              const userInfo = await getUserInfo(id);
              return { 
                id, 
                username: userInfo?.username || '未知用户',
                avatar: userInfo?.avatar || null
              };
            })
          );
          
          // 将后端消息格式转换为前端需要的格式
          const formattedMessages = response.data.data.map(msg => {
            const senderInfo = userInfos.find(u => u.id === msg.senderId);
            
            // 检查是否是图片消息
            const isImageMessage = msg.content && (
              msg.content.startsWith('http') && 
              (msg.content.endsWith('.jpg') || msg.content.endsWith('.jpeg') || msg.content.endsWith('.png') || msg.content.endsWith('.gif') || msg.content.endsWith('.bmp') || msg.content.endsWith('.webp'))
            );
            
            const formattedMsg = {
              messageId: msg.messageId,
              content: msg.content,
              sendTime: msg.createTime,
              senderId: msg.senderId,
              senderName: senderInfo?.username || '未知用户',
              senderAvatar: senderInfo?.avatar || null,
              groupId: msg.groupId,
              messageType: isImageMessage ? 'image' : (msg.messageType || 'text'),
              fileUrl: isImageMessage ? getFullImageUrl(msg.content) : getFullImageUrl(msg.fileUrl),
              fileName: msg.fileName || (isImageMessage ? msg.content.split('/').pop() : null),
              fileSize: msg.fileSize || 0
            };
            return formattedMsg;
          }).sort((a, b) => new Date(a.sendTime) - new Date(b.sendTime));
          
          messages.value = formattedMessages;
          console.log('最终的消息列表:', messages.value);
          
          nextTick(() => scrollToBottom());
        } else {
          throw new Error(response.data?.msg || '获取历史消息失败');
        }
      } catch (error) {
        console.error('加入群组或获取历史消息失败:', error);
        showNotification('加入群组或获取历史消息失败: ' + error.message, 'error');
      } finally {
        loadingHistory.value = false;
      }
    };
    
    // 加入群组
    const joinGroup = async (groupId) => {
      if (!isConnected.value) {
        showNotification('尚未连接到服务器', 'error');
        return;
      }
      
      try {
        await connection.value.invoke('JoinGroup', userId.value, username.value, groupId);
        console.log(`成功加入群组 ${groupId}`);
      } catch (error) {
        // 忽略用户已在群组中的错误
        if (error.message && error.message.includes('用户已在群组中')) {
          console.log(`用户已在群组 ${groupId} 中`);
          return;
        }
        console.error('加入群组失败:', error);
        showNotification('加入群组失败: ' + error, 'error');
      }
    };
    
    // 发送消息
    const sendMessage = async () => {
      if (!isConnected.value || !currentGroup.value) {
        showNotification('未连接到服务器或未选择群组', 'error');
        return;
      }
      
      if (!messageText.value.trim()) {
          return;
        }
      
      try {
        await connection.value.invoke('SendMessageToGroup', messageText.value);
        messageText.value = '';
        messageInput.value.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
        showNotification('发送消息失败: ' + error, 'error');
      }
    };
    
    // 创建群组
    const createGroup = async () => {
      if (!newGroup.value.groupName.trim()) {
        showNotification('请输入群组名称', 'warning');
        return;
      }
      
      try {
        const response = await axios.post(`${window.apiBaseUrl}/api/group`, {
          groupName: newGroup.value.groupName,
          description: newGroup.value.description,
          creatorId: userId.value
        });
        
        showCreateGroupModal.value = false;
        newGroup.value = { groupName: '', description: '' };
        showNotification('群组创建成功', 'success');
        
        // 重新加载群组列表
        await loadUserGroups();
      } catch (error) {
        console.error('创建群组失败:', error);
        showNotification('创建群组失败: ' + (error.response?.data || error.message), 'error');
      }
    };
    
    // 添加用户到群组
    const addUserToGroup = async () => {
      if (!newMemberId.value) {
        showNotification('请输入用户ID', 'warning');
        return;
      }
      
      try {
        await axios.post(`${window.apiBaseUrl}/api/group/${currentGroup.value.groupId}/member`, {
          userId: newMemberId.value
        });
        
        showAddUserModal.value = false;
        newMemberId.value = null;
        showNotification('添加成员成功', 'success');
        
        // 更新群组成员数
        currentGroup.value.memberCount++;
      } catch (error) {
        console.error('添加成员失败:', error);
        showNotification('添加成员失败: ' + (error.response?.data || error.message), 'error');
      }
    };
    
    // 从群组移除用户
    const removeUserFromGroup = async () => {
      if (!removeMemberId.value) {
        showNotification('请输入用户ID', 'warning');
        return;
      }
      
      try {
        await axios.delete(`${window.apiBaseUrl}/api/group/${currentGroup.value.groupId}/member/${removeMemberId.value}`);
        
        showRemoveUserModal.value = false;
        removeMemberId.value = null;
        showNotification('移除成员成功', 'success');
        
        // 更新群组成员数
        currentGroup.value.memberCount--;
      } catch (error) {
        console.error('移除成员失败:', error);
        showNotification('移除成员失败: ' + (error.response?.data || error.message), 'error');
      }
    };
    
    // 表情相关方法
    const insertEmoji = (emoji) => {
      messageText.value += emoji;
      nextTick(() => {
        messageInput.value.focus();
      });
      showEmojiPanel.value = false;
    };
    
    const toggleEmojiPanel = () => {
      showEmojiPanel.value = !showEmojiPanel.value;
    };
    
    // 文件上传相关方法
    const handleImageUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        showNotification('图片上传中...', 'info');
        
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        if (response.data && response.data.url) {
          await connection.value.invoke(
            'SendImageToGroup', 
            response.data.url, 
            response.data.fileName, 
            response.data.fileSize
          );
          
          imageInput.value.value = '';
          showNotification('图片发送成功', 'success');
        } else {
          throw new Error('图片上传失败：服务器返回数据格式不正确');
        }
      } catch (error) {
        console.error('图片上传失败:', error);
        showNotification('图片上传失败: ' + (error.response?.data?.message || error.message), 'error');
        imageInput.value.value = '';
      }
    };
    
    const triggerImageUpload = () => {
      imageInput.value.click();
    };
    
    const handleFileUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        showNotification('文件上传中...', 'info');
        
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        await connection.value.invoke(
          'SendFileToGroup', 
          currentGroup.value.groupId,
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        fileInput.value.value = '';
        showNotification('文件发送成功', 'success');
      } catch (error) {
        console.error('文件上传失败:', error);
        showNotification('文件上传失败: ' + (error.response?.data || error.message), 'error');
        fileInput.value.value = '';
      }
    };
    
    // 图片预览相关方法
    const previewImage = (url) => {
      previewImageUrl.value = url;
    };
    
    const closeImagePreview = () => {
      previewImageUrl.value = null;
    };
    
    // 文件下载
    const downloadFile = (url, fileName) => {
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    };
    
    // 滚动相关方法
    const scrollToBottom = () => {
      if (messagesContainer.value) {
        messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
        hasNewMessage.value = false;
      }
    };
    
    const checkScrollPosition = () => {
      if (messagesContainer.value) {
        const { scrollTop, scrollHeight, clientHeight } = messagesContainer.value;
        isAtBottom.value = Math.abs(scrollHeight - scrollTop - clientHeight) < 50;
      }
    };
    
    // 退出聊天室
    const leaveRoom = () => {
      if (connection.value && currentGroup.value) {
        // 先通知服务器用户离开群组
        try {
          // 执行离开群组的逻辑
          connection.value.invoke("LeaveGroup", currentGroup.value.groupId)
            .catch(err => console.error("离开群组失败:", err))
            .finally(() => {
              // 停止连接
              connection.value.stop();
              
              // 导航到主页
              router.push('/home');
            });
        } catch (error) {
          console.error("离开群组时出错:", error);
          connection.value.stop();
          router.push('/home');
        }
      } else {
        router.push('/home');
      }
    };
    
    // 显示通知
    const showNotification = (message, type = 'info') => {
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
      
      notification.value = {
        show: true,
        message,
        type,
        timeout: setTimeout(() => {
          notification.value.show = false;
        }, 3000)
      };
    };
    
    // 格式化时间
    const formatTime = (dateString) => {
      const date = new Date(dateString);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 格式化日期
    const formatDate = (dateString) => {
      const date = new Date(dateString);
      const today = new Date();
      const yesterday = new Date(today);
      yesterday.setDate(yesterday.getDate() - 1);
      
      if (date.toDateString() === today.toDateString()) {
        return '今天';
      } else if (date.toDateString() === yesterday.toDateString()) {
        return '昨天';
      } else {
        return date.toLocaleDateString('zh-CN', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        });
      }
    };
    
    // 格式化文件大小
    const formatFileSize = (bytes) => {
      if (!bytes) return '未知大小';
      
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      if (bytes === 0) return '0 B';
      
      const i = Math.floor(Math.log(bytes) / Math.log(1024));
      return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
    };
    
    // 获取消息类名
    const getMessageClass = (message) => {
      const classes = [];
      
      if (message.messageType === 'system') {
        classes.push('system-message-container');
      } else {
        classes.push(message.senderId === userId.value ? 'self-message-container' : 'other-message-container');
      }
      
      return classes.join(' ');
    };
    
    // 判断是否显示日期分隔线
    const shouldShowDateSeparator = (index) => {
      if (index === 0) return true;
      
      const currentDate = new Date(messages.value[index].sendTime).setHours(0, 0, 0, 0);
      const prevDate = new Date(messages.value[index - 1].sendTime).setHours(0, 0, 0, 0);
      
      return currentDate !== prevDate;
    };
    
    // 处理群组搜索
    const handleGroupSearch = async () => {
      try {
        if (!groupSearch.value.trim()) {
          // 如果搜索框为空，获取所有群组
          await loadUserGroups();
        } else {
          // 调用搜索API
          const response = await axios.get(`${window.apiBaseUrl}/api/Group/search?groupName=${encodeURIComponent(groupSearch.value)}&userId=${userId.value}`);
          if (response.data && response.data.code === 200) {
            groups.value = response.data.data;
          } else {
            throw new Error(response.data?.msg || '搜索群组失败');
          }
        }
      } catch (error) {
        console.error('搜索群组失败:', error);
        showNotification('搜索群组失败: ' + error.message, 'error');
      }
    };
    
    // 添加防抖函数
    const debounce = (fn, delay) => {
      let timer = null;
      return function (...args) {
        if (timer) clearTimeout(timer);
        timer = setTimeout(() => {
          fn.apply(this, args);
        }, delay);
      };
    };

    // 使用防抖处理搜索
    const debouncedSearch = debounce(handleGroupSearch, 300);

    // 监听搜索输入
    watch(groupSearch, () => {
      debouncedSearch();
    });
    
    // 添加图片加载错误处理
    const handleImageError = (event) => {
      event.target.src = '/images/image-error.png'; // 替换为默认的错误图片
      event.target.classList.add('image-load-error');
    };
    
    // 组件挂载时
    onMounted(() => {
      if (!userId.value || !username.value) {
        router.push('/login');
        return;
      }
      
      createConnection();
      
      if (messagesContainer.value) {
        messagesContainer.value.addEventListener('scroll', checkScrollPosition);
      }
      
      document.addEventListener('click', (event) => {
        const emojiButton = document.querySelector('.emoji-button');
        const emojiPanel = document.querySelector('.emoji-panel');
        
        if (emojiButton && emojiPanel && 
            !emojiButton.contains(event.target) && 
            !emojiPanel.contains(event.target)) {
          showEmojiPanel.value = false;
        }
      });
      
      // 加载好友列表
      loadFriendsList();
    });
    
    // 组件卸载前
    onBeforeUnmount(() => {
      if (connection.value) {
        connection.value.stop();
      }
      
      if (messagesContainer.value) {
        messagesContainer.value.removeEventListener('scroll', checkScrollPosition);
      }
      
      document.removeEventListener('click', () => {});
      
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
    });
    
    // 监听消息列表变化
    watch(messages, () => {
      if (isAtBottom.value) {
        nextTick(() => scrollToBottom());
      }
    });
    
    // 处理图片URL的全局函数
    const getFullImageUrl = (url) => {
      if (!url) return null;
      if (url.startsWith('http')) return url;
      return `${window.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    };

    return {
      // 用户信息
      userId,
      username,
      userAvatar,
      
      // 群组信息
      groups,
      currentGroup,
      groupSearch,
      
      // 连接状态
      isConnected,
      connectionStatus,
      
      // 消息相关
      messages,
      messageText,
      loadingHistory,
      messagesContainer,
      messageInput,
      hasNewMessage,
      isAtBottom,
      
      // 表情相关
      showEmojiPanel,
      emojis,
      
      // 文件上传相关
      imageInput,
      fileInput,
      
      // 图片预览
      previewImageUrl,
      
      // 通知
      notification,
      
      // 群组管理相关
      showCreateGroupModal,
      showAddUserModal,
      showRemoveUserModal,
      newGroup,
      newMemberId,
      removeMemberId,
      
      // 用户名片相关
      showingUserCard,
      selectedUserId,
      showUserCard,
      closeUserCard,
      isUserFriend,
      handleFriendRequestSent,
      
      // 方法
      selectGroup,
      sendMessage,
      createGroup,
      addUserToGroup,
      removeUserFromGroup,
      insertEmoji,
      handleImageUpload,
      handleFileUpload,
      toggleEmojiPanel,
      previewImage,
      closeImagePreview,
      downloadFile,
      scrollToBottom,
      leaveRoom,
      formatTime,
      formatDate,
      formatFileSize,
      getMessageClass,
      shouldShowDateSeparator,
      handleGroupSearch,
      triggerImageUpload,
      handleImageError,
      getFullImageUrl,
    };
  }
};
</script>

<style scoped>
.chat-room-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fb;
}

.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 25px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.room-info h1 {
  font-size: 20px;
  margin: 0;
  font-weight: 600;
}

.room-status {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 5px;
  font-size: 14px;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: #ff4757;
}

.status-indicator.connected {
  background-color: #4CD964;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 15px;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.default-avatar {
  color: #4776E6;
  font-weight: bold;
  font-size: 18px;
}

.leave-button {
  padding: 8px 16px;
  border: none;
  border-radius: 20px;
  background-color: rgba(255, 255, 255, 0.2);
  color: white;
  cursor: pointer;
  transition: all 0.2s;
}

.leave-button:hover {
  background-color: rgba(255, 255, 255, 0.3);
}

.chat-main {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.sidebar {
  width: 300px;
  background-color: white;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
}

.sidebar-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.sidebar-section {
  margin-bottom: 30px;
}

.sidebar-header {
  margin-bottom: 15px;
}

.sidebar-header h2 {
  font-size: 16px;
  color: #333;
  margin-bottom: 10px;
}

.search-box input {
  width: 100%;
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 20px;
  font-size: 14px;
  transition: all 0.2s;
}

.search-box input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
  outline: none;
}

.groups-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.group-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s;
}

.group-item:hover {
  background-color: #f5f7fb;
}

.group-item.active {
  background-color: #f0f7ff;
}

.group-avatar {
  width: 45px;
  height: 45px;
  border-radius: 10px;
  background-color: #f0f7ff;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
  color: #4776E6;
  font-weight: bold;
  font-size: 18px;
}

.group-details {
  flex: 1;
}

.group-name {
  font-weight: 500;
  color: #333;
  margin-bottom: 4px;
}

.group-members {
  font-size: 12px;
  color: #666;
}

.messages-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: #f5f7fb;
  position: relative;
}

.messages-wrapper {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.message-item {
  margin-bottom: 20px;
}

.date-separator {
  text-align: center;
  margin: 20px 0;
  color: #666;
  font-size: 12px;
}

.system-message {
  text-align: center;
  margin: 10px 0;
  color: #999;
  font-size: 12px;
}

.system-message-content {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background-color: rgba(0, 0, 0, 0.05);
  border-radius: 20px;
  font-size: 13px;
  color: #666;
}

.message-text.unknown-user {
  text-align: center;
  color: #999;
  font-size: 12px;
  background-color: transparent;
  box-shadow: none;
  padding: 4px 8px;
}

.message-text.unknown-user::before {
  content: "来自";
  margin-right: 4px;
  color: #999;
}

.message-text.unknown-user::after {
  content: "的消息";
  margin-left: 4px;
  color: #999;
}

.user-message {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.user-message.self-message {
  flex-direction: row-reverse;
}

.message-avatar, .self-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  background: #e6f7ff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.message-avatar img, .self-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.message-content {
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.user-message.self-message .message-content {
  align-items: flex-end;
  text-align: right;
}

.message-text {
  background: #fff;
  color: #333;
  padding: 10px 16px;
  border-radius: 16px;
  font-size: 15px;
  box-shadow: 0 1px 2px rgba(0,0,0,0.04);
  word-break: break-all;
}

.user-message.self-message .message-text {
  background: #1890ff;
  color: #fff;
}

.message-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
  font-size: 12px;
  color: #666;
}

.self-message .message-info {
  flex-direction: row-reverse;
}

.message-image img {
  max-width: 300px;
  max-height: 200px;
  border-radius: 8px;
  cursor: pointer;
}

.image-info {
  font-size: 12px;
  color: #666;
  margin-top: 4px;
}

.message-file {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px;
  background-color: #f5f7fb;
  border-radius: 8px;
  cursor: pointer;
}

.file-icon {
  width: 40px;
  height: 40px;
  background-color: #e0e0e0;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.file-info {
  flex: 1;
}

.file-name {
  font-weight: 500;
  margin-bottom: 2px;
}

.file-size {
  font-size: 12px;
  color: #666;
}

.chat-footer {
  padding: 15px 25px;
  background-color: white;
  border-top: 1px solid #e0e0e0;
  display: flex;
  align-items: flex-end;
  gap: 15px;
}

.toolbar {
  display: flex;
  gap: 10px;
  padding: 10px;
  border-bottom: 1px solid #e0e0e0;
}

.tool-button {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.2s;
  background-color: #f5f5f5;
}

.tool-button:hover {
  background-color: #e0e0e0;
}

.tool-button.emoji-button {
  position: relative;
}

.tool-button.emoji-button::before {
  content: "😊";
  font-size: 20px;
  line-height: 1;
}

.tool-button.image-button {
  position: relative;
}

.tool-button.image-button::before {
  content: "📷";
  font-size: 20px;
  line-height: 1;
}

.input-container {
  flex: 1;
  position: relative;
}

.message-input {
  width: 100%;
  min-height: 40px;
  max-height: 120px;
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 20px;
  resize: none;
  font-size: 14px;
  line-height: 1.5;
  transition: all 0.2s;
}

.message-input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
  outline: none;
}

.send-button {
  padding: 10px 20px;
  border: none;
  border-radius: 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
}

.send-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.send-button:not(:disabled):hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.emoji-panel {
  position: absolute;
  bottom: 100%;
  left: 0;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  padding: 10px;
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 8px;
  margin-bottom: 10px;
  z-index: 1000;
}

.emoji-item {
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.2s;
  font-size: 20px;
}

.emoji-item:hover {
  background-color: #f5f7fb;
  transform: scale(1.1);
}

.new-message-indicator {
  position: absolute;
  bottom: 20px;
  left: 50%;
  transform: translateX(-50%);
  background-color: #4776E6;
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: all 0.2s;
}

.new-message-indicator:hover {
  transform: translateX(-50%) translateY(-2px);
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
}

.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 12px 20px;
  border-radius: 8px;
  background-color: white;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background-color: white;
  border-radius: 12px;
  width: 400px;
  max-width: 90%;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
}

.modal-header {
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: #666;
  font-size: 20px;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #333;
  font-size: 14px;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  transition: all 0.2s;
}

.form-group input:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
  outline: none;
}

.modal-footer {
  padding: 20px;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.cancel-btn,
.confirm-btn {
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}

.cancel-btn {
  background-color: #f5f5f5;
  border: 1px solid #e0e0e0;
  color: #333;
}

.confirm-btn {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border: none;
  color: white;
}

.cancel-btn:hover {
  background-color: #e0e0e0;
}

.confirm-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.summary-section,
.summary-status,
.summary-content-container,
.auto-summary-content,
.summary-loading,
.summary-empty,
.summary-hint,
.summary-error {
  display: none;
}

.message-image {
  max-width: 300px;
  max-height: 300px;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
}

.message-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.image-preview-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.image-preview-content {
  position: relative;
  max-width: 90%;
  max-height: 90%;
}

.image-preview-content img {
  max-width: 100%;
  max-height: 90vh;
  object-fit: contain;
}

.close-preview {
  position: absolute;
  top: -40px;
  right: 0;
  background: none;
  border: none;
  color: white;
  font-size: 24px;
  cursor: pointer;
  padding: 8px;
}
</style> 