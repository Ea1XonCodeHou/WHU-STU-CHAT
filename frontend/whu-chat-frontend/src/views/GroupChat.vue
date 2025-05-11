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
          
          <!-- AI总结部分 -->
          <div class="sidebar-section summary-section" v-if="currentGroup">
            <div class="sidebar-header">
              <h2>AI实时总结</h2>
              <div class="summary-status" :class="{ 'active': autoSummaryActive }">
                {{ summarizing ? '正在总结...' : autoSummaryActive ? '自动总结已开启' : '自动总结已暂停' }}
              </div>
            </div>
            <div class="summary-content-container">
              <div v-if="summaryError" class="summary-error">
                <p>{{ summaryError }}</p>
              </div>
              <div v-else-if="summarizing && !chatSummary" class="summary-loading">
                <div class="loading-spinner"></div>
                <p>AI正在分析聊天记录...</p>
              </div>
              <div v-else-if="!chatSummary" class="summary-empty">
                <i class="summary-icon"></i>
                <p>暂无总结内容</p>
                <p class="summary-hint">将自动分析最近的聊天内容</p>
              </div>
              <div v-else class="auto-summary-content" v-html="formattedSummary"></div>
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
                <div class="avatar default-avatar">
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
                <div v-if="message.messageType === 'text'" class="message-text">
                  {{ message.content }}
              </div>
              
                <!-- 图片消息 -->
                <div v-else-if="message.messageType === 'image'" class="message-image">
                  <img :src="message.fileUrl" alt="图片消息" @click="previewImage(message.fileUrl)" />
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
              <div class="message-avatar self-avatar" v-if="message.senderId === userId">
                <div class="avatar default-avatar">
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
        <img :src="previewImageUrl" alt="图片预览" />
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
    
    // 聊天总结相关
    const summarizing = ref(false);
    const chatSummary = ref('');
    const summaryError = ref('');
    
    // 自动总结相关
    const autoSummaryActive = ref(true);
    const lastMessageTime = ref(Date.now());
    const autoSummaryInterval = ref(null);
    const summaryDebounceTimeout = ref(null);
    const lastSummaryTime = ref(0);
    const messageCountSinceLastSummary = ref(0);

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
          return response.data.data;
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
        
        // 将后端消息格式转换为前端需要的格式
        const formattedMessage = {
          messageId: message.messageId,
          content: message.content,
          sendTime: message.createTime,
          senderId: message.senderId,
          senderName: senderInfo?.username || '未知用户',
          groupId: message.groupId,
          messageType: 'text' // 默认消息类型为文本
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
        
        // 记录消息活动，用于自动总结功能
        recordMessageActivity();
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
              return { id, username: userInfo?.username || '未知用户' };
            })
          );
          
          // 将后端消息格式转换为前端需要的格式
          const formattedMessages = historyMessages.map(msg => {
            const senderInfo = userInfos.find(u => u.id === msg.senderId);
            return {
              messageId: msg.messageId,
              content: msg.content,
              sendTime: msg.createTime,
              senderId: msg.senderId,
              senderName: senderInfo?.username || '未知用户',
              groupId: msg.groupId,
              messageType: 'text' // 默认消息类型为文本
            };
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
      chatSummary.value = '';
      summaryError.value = '';
      messageCountSinceLastSummary.value = 0;
      
      try {
        // 加入群组
        await joinGroup(group.groupId);
        
        // 获取历史消息
        const response = await axios.get(`${window.apiBaseUrl}/api/Group/${group.groupId}/messages?count=50`);
        if (response.data && response.data.code === 200) {
          // 获取所有发送者的用户信息
          const senderIds = [...new Set(response.data.data.map(msg => msg.senderId))];
          const userInfos = await Promise.all(
            senderIds.map(async (id) => {
              const userInfo = await getUserInfo(id);
              return { id, username: userInfo?.username || '未知用户' };
            })
          );
          
          // 将后端消息格式转换为前端需要的格式
          messages.value = response.data.data.map(msg => {
            const senderInfo = userInfos.find(u => u.id === msg.senderId);
                return {
              messageId: msg.messageId,
              content: msg.content,
              sendTime: msg.createTime,
              senderId: msg.senderId,
              senderName: senderInfo?.username || '未知用户',
              groupId: msg.groupId,
              messageType: 'text' // 默认消息类型为文本
            };
          }).sort((a, b) => new Date(a.sendTime) - new Date(b.sendTime));
          nextTick(() => scrollToBottom());
          
          // 初始请求一次总结
          if (messages.value.length >= 5) {
            setTimeout(() => {
              requestChatSummary(true);
            }, 1000);
          }
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
        
        await connection.value.invoke(
          'SendImageToGroup', 
          currentGroup.value.groupId,
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        imageInput.value.value = '';
        showNotification('图片发送成功', 'success');
      } catch (error) {
        console.error('图片上传失败:', error);
        showNotification('图片上传失败: ' + (error.response?.data || error.message), 'error');
        imageInput.value.value = '';
      }
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
    
    // 请求聊天总结
    const requestChatSummary = async (silent = false) => {
      if (!isConnected.value || !currentGroup.value) {
        if (!silent) {
          showNotification('未连接到服务器或未选择群组', 'error');
        }
        return;
      }
      
      // 检查自上次总结以来的时间
      const now = Date.now();
      if (now - lastSummaryTime.value < 10000 && silent) { // 10秒内不重复自动总结
        return;
      }
      
      // 如果是静默模式（自动总结）且消息计数器少于3条，则不总结
      if (silent && messageCountSinceLastSummary.value < 3) {
        return;
      }
      
      summarizing.value = true;
      summaryError.value = '';
      
      try {
        const response = await axios.post(`${window.apiBaseUrl}/api/ai/summarize`, {
          groupId: currentGroup.value.groupId,
          userId: userId.value,
          username: username.value,
          messageCount: 100
        });
        
        if (response.data && response.data.success) {
          chatSummary.value = response.data.message;
          lastSummaryTime.value = now;
          messageCountSinceLastSummary.value = 0;
        } else {
          summaryError.value = response.data?.error || '生成总结失败，请稍后重试';
          if (!silent) {
            showNotification('总结生成失败: ' + summaryError.value, 'error');
          }
        }
      } catch (error) {
        console.error('获取聊天总结失败:', error);
        summaryError.value = '获取聊天总结失败: ' + (error.response?.data?.error || error.message);
        if (!silent) {
          showNotification('总结生成失败: ' + summaryError.value, 'error');
        }
      } finally {
        summarizing.value = false;
      }
    };
    
    // 自动执行聊天总结
    const setupAutoSummary = () => {
      // 每30秒检查是否需要更新总结
      autoSummaryInterval.value = setInterval(() => {
        if (!autoSummaryActive.value || !currentGroup.value) return;
        
        const inactiveThreshold = 60000; // 1分钟无活动则不自动总结
        const now = Date.now();
        
        if (now - lastMessageTime.value > inactiveThreshold) {
          // 聊天不活跃，不进行总结
          console.log('聊天不活跃，跳过自动总结');
          return;
        }
        
        // 执行自动总结
        requestChatSummary(true);
      }, 30000); // 30秒
    };
    
    // 在收到新消息时记录时间和计数
    const recordMessageActivity = () => {
      lastMessageTime.value = Date.now();
      messageCountSinceLastSummary.value++;
      
      // 防抖处理，当快速收到多条消息时，等待一定时间后再总结
      if (summaryDebounceTimeout.value) {
        clearTimeout(summaryDebounceTimeout.value);
      }
      
      summaryDebounceTimeout.value = setTimeout(() => {
        // 如果收到至少5条新消息，自动触发总结
        if (messageCountSinceLastSummary.value >= 5) {
          requestChatSummary(true);
        }
      }, 5000); // 5秒后检查是否需要总结
    };
    
    // 关闭总结弹窗
    const closeSummaryModal = () => {
      showSummaryModal.value = false;
    };
    
    // 格式化总结内容
    const formattedSummary = computed(() => {
      if (!chatSummary.value) return '';
      
      // 在处理其他格式之前先处理标题和小标题
      let formatted = chatSummary.value;
      
      // 移除可能出现的标记前缀
      formatted = formatted.replace(/H3:/g, '');
      formatted = formatted.replace(/LI:/g, '');
      formatted = formatted.replace(/TITLE/g, '聊天记录总结');
      
      // 标识聊天记录总结标题
      formatted = formatted.replace(/^聊天记录总结.*$/m, '###TITLE###');
      
      // 标识小标题（主要话题、重要观点和信息等）
      formatted = formatted.replace(/^主要话题$/m, '###H3:主要话题###');
      formatted = formatted.replace(/^重要观点和信息$/m, '###H3:重要观点和信息###');
      formatted = formatted.replace(/^提出的问题$/m, '###H3:提出的问题###');
      formatted = formatted.replace(/^达成的共识或结论$/m, '###H3:达成的共识或结论###');
      formatted = formatted.replace(/^补充观察$/m, '###H3:补充观察###');
      
      // 标识列表项
      formatted = formatted.replace(/^- (.*)$/gm, '###LI:$1###');
      
      // 现在将换行符转换为<br>
      formatted = formatted.replace(/\n/g, '<br>');
      
      // 删除多余的标记符号如 #，###, #### 等（除了我们自己添加的###标记）
      formatted = formatted.replace(/(?<!###)#+\s+/g, '');
      formatted = formatted.replace(/\s*(?<!###)#+(?!###)/g, '');
      
      // 转换我们之前标记的内容
      formatted = formatted.replace(/###TITLE###/, '<h2>聊天记录总结</h2>');
      formatted = formatted.replace(/###H3:(.*?)###/g, '<h3>$1</h3>');
      
      // 将标记的列表项转换为HTML列表项
      formatted = formatted.replace(/###LI:(.*?)###/g, '<li>$1</li>');
      
      // 包装列表项到无序列表中
      if (formatted.includes('<li>')) {
        let parts = formatted.split('<h3>');
        for (let i = 1; i < parts.length; i++) {
          const headingEnd = parts[i].indexOf('</h3>');
          if (headingEnd !== -1) {
            const afterHeading = parts[i].substring(headingEnd + 5);
            if (afterHeading.includes('<li>')) {
              // 使用非贪婪匹配确保正确地将所有列表项包装在ul标签中
              const withList = parts[i].substring(0, headingEnd + 5) + 
                              '<ul>' + 
                              afterHeading.replace(/(<li>.*?<\/li>)+/g, match => match) + 
                              '</ul>';
              parts[i] = withList;
            }
          }
        }
        formatted = parts.join('<h3>');
      }
      
      // 将Markdown风格的粗体和斜体转换为HTML标签
      formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
      formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');
      
      // 美化注释部分
      formatted = formatted.replace(/注：(.*?)(?:<br>|$)/g, '<div class="summary-note"><strong>注：</strong>$1</div>');
      
      return formatted;
    });
    
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
      
      // 设置自动总结
      setupAutoSummary();
      
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
      
      // 清除自动总结相关的定时器
      if (autoSummaryInterval.value) {
        clearInterval(autoSummaryInterval.value);
      }
      
      if (summaryDebounceTimeout.value) {
        clearTimeout(summaryDebounceTimeout.value);
      }
    });
    
    // 监听消息列表变化
    watch(messages, () => {
      if (isAtBottom.value) {
        nextTick(() => scrollToBottom());
      }
    });
    
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
      
      // 聊天总结相关
      summarizing,
      chatSummary,
      summaryError,
      autoSummaryActive,
      
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
      requestChatSummary,
      handleGroupSearch,
      formattedSummary
    };
  }
};
</script>

<style scoped>
/* 聊天室容器 */
.chat-room-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fb;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

/* 头部样式 */
.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.room-info h1 {
  font-size: 24px;
  font-weight: 600;
  margin-bottom: 5px;
}

.room-status {
  display: flex;
  align-items: center;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.8);
}

.status-indicator {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #ff4757;
  margin-right: 8px;
}

.status-indicator.connected {
  background-color: #4CD964;
}

.online-count {
  margin-left: 15px;
}

.user-info {
  display: flex;
  align-items: center;
}

.username {
  margin-right: 15px;
  font-weight: 500;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #4776E6;
  font-weight: bold;
  font-size: 18px;
  margin-right: 15px;
}

.avatar img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.leave-button {
  background-color: rgba(255, 255, 255, 0.1);
  color: white;
  border: none;
  padding: 8px 15px;
  border-radius: 20px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.leave-button:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

/* 主内容区样式 */
.chat-main {
  flex: 1;
  display: flex;
  overflow: hidden;
}

/* 侧边栏样式 */
.sidebar {
  width: 280px;
  background-color: white;
  border-right: 1px solid #eee;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  z-index: 5;
}

.sidebar-content {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.sidebar-section {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: flex 0.3s ease;
  position: relative;
}

.groups-section {
  flex: 1;
  min-height: 200px;
  max-height: 50%;
  border-bottom: 1px solid #eee;
  overflow: hidden;
}

.summary-section {
  flex: 1;
  min-height: 200px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-header {
  padding: 15px;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  flex-direction: column;
  align-items: flex-start;
  flex-shrink: 0;
  background-color: #fafafa;
  z-index: 2;
}

.sidebar-header h2 {
  margin: 0 0 10px 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.search-box {
  width: 100%;
  margin-top: 8px;
}

.search-box input {
  width: 100%;
  padding: 6px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 13px;
  transition: all 0.3s ease;
}

.search-box input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
  outline: none;
}

.group-item {
  display: flex;
  align-items: center;
  padding: 12px 15px;
  margin: 0;
  cursor: pointer;
  transition: background-color 0.2s;
  border-bottom: 1px solid #eee;
}

.group-item:hover {
  background-color: #f5f5f5;
}

.group-item.active {
  background-color: #e6f0ff;
}

.group-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #f0f0f0;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
  color: #4776E6;
  font-weight: bold;
  font-size: 16px;
  flex-shrink: 0;
}

.group-details {
  flex: 1;
  min-width: 0;
}

.group-name {
  font-weight: 500;
  margin-bottom: 2px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.group-members {
  font-size: 12px;
  color: #666;
}

.empty-groups {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 200px;
  color: #999;
  padding: 20px 0;
}

.empty-groups i {
  font-size: 40px;
  margin-bottom: 10px;
  opacity: 0.5;
}

/* 底部输入区样式 */
.chat-footer {
  background-color: white;
  padding: 15px;
  border-top: 1px solid #eee;
  display: flex;
  align-items: flex-end;
}

.toolbar {
  display: flex;
  margin-right: 15px;
}

.tool-button {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
  cursor: pointer;
  position: relative;
  border-radius: 6px;
  transition: all 0.3s ease;
  background-color: #f0f2f5;
}

.tool-button:hover {
  background-color: #e0e3e9;
  transform: translateY(-2px);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

.emoji-icon, .image-icon, .file-icon {
  width: 24px;
  height: 24px;
  display: block;
}

.emoji-icon {
  background-color: #ffd666;
  border-radius: 50%;
  position: relative;
  box-shadow: 0 2px 4px rgba(255, 214, 102, 0.3);
}

.emoji-icon::before {
  content: "";
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #fff;
  top: 7px;
  left: 7px;
}

.emoji-icon::after {
  content: "";
  position: absolute;
  width: 12px;
  height: 6px;
  border: 2px solid #fff;
  border-top: none;
  border-radius: 0 0 10px 10px;
  bottom: 5px;
  left: 6px;
}

.image-icon {
  background-color: #91d5ff;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(145, 213, 255, 0.3);
}

.image-icon::before {
  content: "";
  position: absolute;
  width: 18px;
  height: 14px;
  background-color: #fff;
  border-radius: 2px;
  top: 5px;
  left: 3px;
}

.image-icon::after {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #91d5ff;
  border: 2px solid #fff;
  border-radius: 50%;
  top: 7px;
  left: 5px;
}

.file-icon {
  background-color: #b7eb8f;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(183, 235, 143, 0.3);
}

.file-icon::before {
  content: "";
  position: absolute;
  width: 14px;
  height: 18px;
  background-color: #fff;
  border-radius: 2px;
  top: 3px;
  left: 5px;
}

.file-icon::after {
  content: "";
  position: absolute;
  width: 8px;
  height: 2px;
  background-color: #b7eb8f;
  top: 8px;
  left: 8px;
  box-shadow: 0 3px 0 #b7eb8f, 0 6px 0 #b7eb8f;
}

.file-input {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  cursor: pointer;
}

.input-container {
  flex: 1;
  position: relative;
}

.emoji-panel {
  position: absolute;
  bottom: 50px;
  left: 0;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  padding: 10px;
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 5px;
  max-height: 200px;
  overflow-y: auto;
}

.emoji-item {
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border-radius: 4px;
  transition: background-color 0.2s;
}

.emoji-item:hover {
  background-color: #f5f5f5;
}

.message-input {
  width: 100%;
  min-height: 40px;
  max-height: 120px;
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 20px;
  resize: none;
  outline: none;
  font-size: 14px;
  line-height: 1.5;
  transition: all 0.2s;
}

.message-input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
}

.send-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-left: 10px;
  cursor: pointer;
  transition: all 0.2s;
}

.send-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.send-button:hover:not(:disabled) {
  transform: scale(1.1);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

/* 图片预览样式 */
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
  border-radius: 8px;
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
}

/* 通知样式 */
.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 12px 20px;
  border-radius: 8px;
  color: white;
  font-size: 14px;
  z-index: 1000;
  animation: slideIn 0.3s ease;
}

.notification.info {
  background-color: #4776E6;
}

.notification.success {
  background-color: #4CD964;
}

.notification.warning {
  background-color: #FF9500;
}

.notification.error {
  background-color: #FF3B30;
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

/* 模态窗口样式 */
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
  border-radius: 10px;
  width: 400px;
  max-width: 90%;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eee;
}

.modal-header h3 {
  font-size: 18px;
  color: #333;
}

.close-btn {
  background: none;
  border: none;
  color: #666;
  font-size: 20px;
  cursor: pointer;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  font-size: 14px;
  color: #333;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  transition: all 0.2s;
}

.form-group input:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  padding: 20px;
  border-top: 1px solid #eee;
  gap: 10px;
}

.cancel-btn {
  background-color: #f5f5f5;
  color: #666;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-btn:hover {
  background-color: #e0e0e0;
}

.confirm-btn {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.confirm-btn:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

/* 新消息提示 */
.new-message-indicator {
  position: fixed;
  bottom: 80px;
  left: 50%;
  transform: translateX(-50%);
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  padding: 8px 15px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  cursor: pointer;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  animation: bounce 2s infinite;
}

.new-message-indicator i {
  margin-right: 5px;
}

@keyframes bounce {
  0%, 20%, 50%, 80%, 100% {
    transform: translateY(0) translateX(-50%);
  }
  40% {
    transform: translateY(-10px) translateX(-50%);
  }
  60% {
    transform: translateY(-5px) translateX(-50%);
  }
}

/* 总结按钮样式 */
.summary-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 8px 15px;
  border-radius: 20px;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.2s;
  margin-right: 15px;
}

.summary-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.summary-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.summary-icon {
  margin-right: 5px;
}

/* 群组相关样式 */
.empty-groups i {
  font-size: 40px;
  margin-bottom: 10px;
  opacity: 0.5;
}

/* 总结部分样式 */
.summary-status {
  font-size: 12px;
  color: #999;
  padding: 3px 8px;
  border-radius: 10px;
  background-color: #f5f5f5;
}

.summary-status.active {
  color: #52c41a;
  background-color: #f6ffed;
}

.summary-section .sidebar-header {
  flex-direction: row;
  justify-content: space-between;
  align-items: center;
}

.summary-section .sidebar-header h2 {
  margin: 0;
}

.groups-list {
  flex: 1;
  overflow-y: auto;
  padding: 0;
  scrollbar-width: thin;
  scrollbar-color: #ddd #f5f5f5;
}

.groups-list::-webkit-scrollbar {
  width: 6px;
}

.groups-list::-webkit-scrollbar-track {
  background: #f5f5f5;
}

.groups-list::-webkit-scrollbar-thumb {
  background-color: #ddd;
  border-radius: 3px;
}

.summary-content-container {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
  position: relative;
  scrollbar-width: thin;
  scrollbar-color: #ddd #f5f5f5;
}

.summary-content-container::-webkit-scrollbar {
  width: 6px;
}

.summary-content-container::-webkit-scrollbar-track {
  background: #f5f5f5;
}

.summary-content-container::-webkit-scrollbar-thumb {
  background-color: #ddd;
  border-radius: 3px;
}

.auto-summary-content {
  color: #333;
  line-height: 1.6;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

.auto-summary-content h2 {
  margin-top: 0;
  font-size: 18px;
  color: #4776E6;
  margin-bottom: 15px;
  text-align: center;
  font-weight: 600;
  border-bottom: 2px solid #4776E6;
  padding-bottom: 10px;
  position: relative;
}

.auto-summary-content h3 {
  font-size: 16px;
  color: #fff;
  margin-top: 15px;
  margin-bottom: 10px;
  font-weight: 600;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  padding: 8px 12px;
  border-radius: 6px;
  box-shadow: 0 2px 6px rgba(71, 118, 230, 0.2);
  position: relative;
  padding-left: 15px;
  display: flex;
  align-items: center;
}

.auto-summary-content ul {
  margin: 0 0 15px 0;
  padding: 0;
  background-color: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  border: 1px solid #eee;
}

.auto-summary-content li {
  margin: 0;
  padding: 8px 15px 8px 30px;
  position: relative;
  list-style-type: none;
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s ease;
  font-size: 14px;
}

.auto-summary-content li:last-child {
  border-bottom: none;
}

.auto-summary-content li:hover {
  background-color: #f0f7ff;
}

.auto-summary-content li::before {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #4776E6;
  left: 15px;
  top: 14px;
  border-radius: 50%;
}

.summary-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 100px;
  padding: 20px 0;
}

.summary-loading .loading-spinner {
  width: 30px;
  height: 30px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #4776E6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 15px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.summary-loading p {
  color: #999;
  font-size: 14px;
}

.summary-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 150px;
  color: #999;
  padding: 20px 0;
}

.summary-empty i {
  font-size: 40px;
  margin-bottom: 15px;
  opacity: 0.5;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%23bbb'%3E%3Cpath d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-2 17H8v-2h4v2zm6-4H6v-2h12v2zm0-4H6v-2h12v2z'/%3E%3C/svg%3E");
  width: 48px;
  height: 48px;
  background-size: contain;
  background-repeat: no-repeat;
}

.summary-empty p {
  margin: 5px 0;
  text-align: center;
}

.summary-hint {
  font-size: 12px;
  color: #bbb;
  margin-top: 5px;
}

.summary-error {
  background-color: #fff5f5;
  border-radius: 8px;
  padding: 15px;
  color: #cf1322;
  font-size: 14px;
  margin: 10px 0;
}
</style> 