<template>
  <div class="chat-app">
    <!-- 左侧边栏 - 对话历史 -->
    <div class="sidebar sidebar-left" :class="{ 'sidebar-closed': !showHistorySidebar }">
      <div class="sidebar-header">
        <h3>对话历史</h3>
        <button class="icon-button" @click="toggleHistorySidebar">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M15 6l-6 6 6 6" />
          </svg>
        </button>
      </div>
      <div class="sidebar-content">
        <div v-if="chatHistory.length === 0" class="empty-history">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p>暂无对话历史</p>
        </div>
        <div v-else class="history-list">
          <div 
            v-for="(chat, index) in chatHistory" 
            :key="index" 
            class="history-item"
            @click="loadChatHistory(chat.id)"
          >
            <div class="history-item-title">{{ chat.title || '新对话' }}</div>
            <div class="history-item-time">{{ formatHistoryDate(chat.timestamp) }}</div>
          </div>
        </div>
      </div>
      <div class="sidebar-footer">
        <button class="new-chat-button" @click="startNewChat">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 4v16m-8-8h16" />
          </svg>
          新对话
        </button>
      </div>
    </div>

    <!-- 主对话区域 -->
    <div class="main-content">
      <!-- 顶部导航栏 -->
      <div class="chat-header">
        <div class="header-left">
          <!-- 历史菜单按钮 -->
          <button class="icon-button" @click="toggleHistorySidebar">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <router-link to="/home" class="back-button">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M15 19l-7-7 7-7" />
            </svg>
            返回主页
          </router-link>
        </div>

        <div class="header-title">
          <div class="current-model-info">
            <div class="model-avatar" :style="{ backgroundColor: currentModel.color }">
              {{ currentModel.shortName }}
            </div>
            <div class="model-details">
              <h1>{{ currentModel.name }}</h1>
              <div class="model-tag" :class="{ 'pro-tag': currentModel.isPro }">
                {{ currentModel.isPro ? 'PRO' : '基础版' }}
              </div>
            </div>
          </div>
        </div>

        <div class="header-right">
          <!-- 设置菜单按钮 -->
          <button class="icon-button" @click="toggleModelSidebar">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 15V3m0 12l-4 4m4-4l4 4M2 12h20" />
            </svg>
          </button>
          <button class="clear-button" @click="confirmClearHistory">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
            清空对话
          </button>
        </div>
      </div>
      
      <!-- 聊天内容区域 -->
      <div class="chat-content" ref="chatContent">
        <!-- 欢迎消息 -->
        <div v-if="messages.length === 0" class="welcome-container">
          <div class="welcome-header">
            <div class="welcome-avatar" :style="{ backgroundColor: currentModel.color }">
              {{ currentModel.shortName }}
            </div>
            <h2>欢迎使用武汉大学智能助手</h2>
          </div>
          <div class="welcome-message">
            <p>你好，我是武汉大学的智能助手。我可以回答你关于武汉大学的问题，也可以帮助你解决学习和生活中的困惑。</p>
          </div>
          
          <div class="model-description">
            <h3>当前使用模型: {{ currentModel.name }}</h3>
            <p>{{ currentModel.description }}</p>
            <div class="upgrade-prompt" v-if="!userIsPro">
              <p>升级到会员可解锁更多高级模型和功能</p>
              <button class="upgrade-button" @click="showUpgradeModal = true">
                升级会员
              </button>
            </div>
          </div>

          <div class="suggestions">
            <div class="suggestion-title">你可以问我这些问题：</div>
            <div class="suggestion-items">
              <div class="suggestion-item" v-for="(item, index) in suggestions" :key="index" @click="useQuestion(item)">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M8 12h.01M12 12h.01M16 12h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                {{ item }}
              </div>
            </div>
          </div>
        </div>
        
        <!-- 聊天消息 -->
        <div v-for="(message, index) in messages" :key="index" 
             class="message-container" 
             :class="{ 'user-container': message.role === 'user', 'ai-container': message.role === 'assistant' }">
          <div class="message-header">
            <div class="message-avatar" 
                 :class="{ 'user-avatar': message.role === 'user' }"
                 :style="message.role === 'assistant' ? { backgroundColor: currentModel.color } : {}">
              {{ message.role === 'user' ? username.charAt(0).toUpperCase() : currentModel.shortName }}
            </div>
            <div class="message-info">
              <div class="message-name">{{ message.role === 'user' ? username : currentModel.name }}</div>
              <div class="message-time">{{ formatTime(message.timestamp) }}</div>
            </div>
          </div>
          <div class="message-body" v-html="formatMessage(message.content)"></div>
          <div class="message-actions" v-if="message.role === 'assistant'">
            <button class="action-button copy-button" @click="copyToClipboard(message.content)" title="复制回答">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
              </svg>
            </button>
            <button class="action-button regenerate-button" @click="regenerateResponse(index)" title="重新生成" :disabled="loading">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            </button>
          </div>
        </div>
        
        <!-- 加载动画 -->
        <div v-if="loading" class="typing-container">
          <div class="typing-avatar" :style="{ backgroundColor: currentModel.color }">
            {{ currentModel.shortName }}
          </div>
          <div class="typing-content">
            <div class="typing-name">{{ currentModel.name }}</div>
            <div class="typing-indicator">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        </div>
      </div>
      
      <!-- 输入区域 -->
      <div class="chat-input">
        <div class="chat-input-container">
          <textarea 
            v-model="messageText" 
            placeholder="输入问题..."
            ref="inputField"
            @keydown.ctrl.enter.prevent="addNewLine"
            @keydown.enter.prevent="sendMessage"
            :disabled="loading"
          ></textarea>
          <div class="input-actions">
            <button class="feature-button" @click="showAdvancedFeatures" title="高级功能">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
              </svg>
            </button>
            <button class="feature-button" @click="attachFile" title="上传文件">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
              </svg>
            </button>
          </div>
          <button 
            class="send-button" 
            @click="sendMessage" 
            :disabled="loading || !messageText.trim()"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M5 12h14M12 5l7 7-7 7" />
            </svg>
          </button>
        </div>
        <div class="input-features">
          <div class="model-info">
            使用模型: {{ currentModel.name }}
            <span class="model-change" @click="toggleModelSidebar">更换</span>
          </div>
          <div class="input-tips">
            按 Enter 发送，Ctrl+Enter 换行
          </div>
        </div>
      </div>
    </div>

    <!-- 右侧边栏 - 模型选择 -->
    <div class="sidebar sidebar-right" :class="{ 'sidebar-closed': !showModelSidebar }">
      <div class="sidebar-header">
        <h3>模型选择</h3>
        <button class="icon-button" @click="toggleModelSidebar">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M9 6l6 6-6 6" />
          </svg>
        </button>
      </div>
      <div class="sidebar-content">
        <!-- 会员状态显示 -->
        <div class="membership-status">
          <div class="membership-badge" :class="{
            'level-normal': userMembershipLevel === 'normal',
            'level-vip': userMembershipLevel === 'vip',
            'level-svip': userMembershipLevel === 'svip'
          }">
            {{ userMembershipLevel === 'normal' ? '普通用户' : userMembershipLevel === 'vip' ? 'VIP会员' : 'SVIP会员' }}
          </div>
          <button 
            v-if="userMembershipLevel === 'normal'" 
            class="upgrade-link"
            @click="showMembershipBenefits"
          >
            升级会员
          </button>
        </div>
        
        <div class="model-list">
          <div 
            v-for="(model, index) in availableModels" 
            :key="index" 
            class="model-item"
            :class="{ 
              'model-selected': model.id === currentModel.id, 
              'model-locked': !isModelAvailable(model) 
            }"
            @click="selectModel(model)"
          >
            <div class="model-item-avatar" :style="{ backgroundColor: model.color }">
              {{ model.shortName }}
            </div>
            <div class="model-item-details">
              <div class="model-item-name">
                {{ model.name }}
                <span v-if="model.id === 'gpt-3.5'" class="model-item-tag vip-tag">VIP</span>
                <span v-else-if="model.isPro" class="model-item-tag svip-tag">SVIP</span>
              </div>
              <div class="model-item-desc">{{ model.shortDescription }}</div>
            </div>
            <div class="model-item-status">
              <svg v-if="model.id === currentModel.id" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M5 13l4 4L19 7" />
              </svg>
              <svg v-else-if="!isModelAvailable(model)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
            </div>
          </div>
        </div>
      </div>
      <div class="sidebar-footer" v-if="userMembershipLevel === 'normal'">
        <div class="upgrade-card">
          <div class="upgrade-title">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            升级会员解锁全部模型
          </div>
          <button class="upgrade-button" @click="showUpgradeModal = true">
            立即升级
          </button>
        </div>
      </div>
    </div>

    <!-- 清空对话确认弹窗 -->
    <div class="modal-overlay" v-if="showClearConfirm" @click="showClearConfirm = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>确认清空对话</h3>
        </div>
        <div class="modal-body">
          <p>确定要清空所有聊天记录吗？此操作不可恢复。</p>
        </div>
        <div class="modal-footer">
          <button class="modal-cancel" @click="showClearConfirm = false">取消</button>
          <button class="modal-confirm" @click="clearHistory">确认清空</button>
        </div>
      </div>
    </div>

    <!-- 会员升级弹窗 -->
    <div class="modal-overlay" v-if="showUpgradeModal" @click="showUpgradeModal = false">
      <div class="modal-content modal-upgrade" @click.stop>
        <div class="modal-header">
          <h3>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            会员特权
          </h3>
          <button class="icon-button" @click="showUpgradeModal = false">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="modal-body">
          <div class="upgrade-content">
            <div class="upgrade-illustration">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="60" height="60" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
              </svg>
            </div>
            <h4>选择适合您的会员等级</h4>
            
            <div class="membership-plans">
              <div class="membership-plan" :class="{ 'current-plan': userMembershipLevel === 'vip' }">
                <div class="plan-name">VIP会员</div>
                <div class="plan-price">¥9.9/月</div>
                <ul class="plan-features">
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    使用GPT-3.5模型
                  </li>
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    每天100次对话额度
                  </li>
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    基础文件上传分析
                  </li>
                </ul>
                <button class="plan-button" @click="userMembershipLevel === 'vip' ? null : $router.push('/member-benefits')">
                  {{ userMembershipLevel === 'vip' ? '当前等级' : '立即开通' }}
                </button>
              </div>
              
              <div class="membership-plan" :class="{ 'current-plan': userMembershipLevel === 'svip' }">
                <div class="plan-tag">推荐</div>
                <div class="plan-name">SVIP会员</div>
                <div class="plan-price">¥19.9/月</div>
                <ul class="plan-features">
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    使用所有高级AI模型
                  </li>
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    无限次AI对话
                  </li>
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    高级文件和图像分析
                  </li>
                  <li>
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M5 13l4 4L19 7" />
                    </svg>
                    优先使用最新功能
                  </li>
                </ul>
                <button class="plan-button premium-button" @click="userMembershipLevel === 'svip' ? null : $router.push('/member-benefits')">
                  {{ userMembershipLevel === 'svip' ? '当前等级' : '立即开通' }}
                </button>
              </div>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="modal-cancel" @click="showUpgradeModal = false">稍后再说</button>
          <button class="modal-confirm modal-upgrade-btn" @click="$router.push('/member-benefits')">
            前往会员中心
          </button>
        </div>
      </div>
    </div>
    
    <!-- 高级功能提示弹窗 -->
    <div class="modal-overlay" v-if="showAdvancedFeatureModal" @click="showAdvancedFeatureModal = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            会员权益未解锁
          </h3>
        </div>
        <div class="modal-body">
          <p>此功能需要升级会员才能使用，升级后即可体验。</p>
        </div>
        <div class="modal-footer">
          <button class="modal-cancel" @click="showAdvancedFeatureModal = false">取消</button>
          <button class="modal-confirm" @click="showUpgrade">升级会员</button>
        </div>
      </div>
    </div>
    
    <!-- 复制成功提示 -->
    <div class="copy-toast" v-if="showCopyToast">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M5 13l4 4L19 7" />
      </svg>
      已复制到剪贴板
    </div>
  </div>
</template>

<script>
import { ref, onMounted, nextTick, watch, computed } from 'vue';
import axios from 'axios';
import { useRoute } from 'vue-router';

export default {
  name: 'AIChat',
  props: {
    userId: {
      type: Number,
      required: true
    },
    username: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const messages = ref([]);
    const messageText = ref('');
    const loading = ref(false);
    const chatContent = ref(null);
    const inputField = ref(null);
    const showClearConfirm = ref(false);
    const showUpgradeModal = ref(false);
    const showAdvancedFeatureModal = ref(false);
    const showCopyToast = ref(false);
    const showHistorySidebar = ref(false);
    const showModelSidebar = ref(false);
    const route = useRoute();
    
    // 获取用户信息，优先使用props，如果没有则从route.query或localStorage获取
    const userId = ref(props.userId || Number(route.query.userId) || Number(localStorage.getItem('userId')));
    const username = ref(props.username || route.query.username || localStorage.getItem('username'));
    
    // 用户会员等级 ("normal", "vip", "svip")
    const userMembershipLevel = ref('normal');
    
    // 基于会员等级的计算属性
    const userIsPro = computed(() => userMembershipLevel.value !== 'normal');
    const userIsSVIP = computed(() => userMembershipLevel.value === 'svip');
    
    // 检查模型是否可用
    const isModelAvailable = (model) => {
      if (!model.isPro) return true; // 基础模型对所有用户开放
      if (model.id === 'gpt-3.5' && (userMembershipLevel.value === 'vip' || userMembershipLevel.value === 'svip')) return true;
      if (userMembershipLevel.value === 'svip') return true; // SVIP用户可以使用所有模型
      return false;
    };
    
    // 获取用户会员信息
    const fetchUserMembership = async () => {
      try {
        const response = await axios.get(
          `${window.apiBaseUrl || 'http://localhost:5067'}/api/membership/current?userId=${userId.value}`
        );
        
        if (response.data) {
          // 根据后端返回的会员信息设置用户等级
          if (response.data.level === 2) {
            userMembershipLevel.value = 'svip';
          } else if (response.data.level === 1) {
            userMembershipLevel.value = 'vip';
          } else {
            userMembershipLevel.value = 'normal';
          }
          console.log('用户会员等级:', userMembershipLevel.value);
        }
      } catch (error) {
        console.error('获取会员信息失败:', error);
        userMembershipLevel.value = 'normal'; // 默认为普通用户
      }
    };
    
    // 聊天历史记录
    const chatHistory = ref([]);
    
    // 可用的AI模型列表
    const availableModels = ref([
      {
        id: 'deepseek-v3',
        name: 'Deepseek 助手',
        shortName: 'DS',
        shortDescription: '基础智能助手，可回答一般性问题',
        description: 'Deepseek 是一款基础智能助手，可以回答一般性问题、提供学习辅导和校园信息查询。适合日常使用。',
        color: '#4e89e8',
        isPro: false
      },
      {
        id: 'gpt-3.5',
        name: 'GPT 3.5',
        shortName: 'G3',
        shortDescription: '强大通用模型，反应速度快',
        description: 'GPT 3.5 是一款强大的通用智能模型，反应速度快，擅长解决各类问题和创意写作。',
        color: '#10a37f',
        isPro: true
      },
      {
        id: 'gpt-4o',
        name: 'GPT 4o',
        shortName: 'G4',
        shortDescription: '高级智能模型，思维逻辑更强',
        description: 'GPT 4o 是最先进的AI语言模型之一，具有强大的理解能力和推理能力，可以处理复杂问题。',
        color: '#ab68ff',
        isPro: true
      },
      {
        id: 'claude-3',
        name: 'Claude 3',
        shortName: 'C3',
        shortDescription: '专注学术研究，文档分析能力强',
        description: 'Claude 3 在学术研究和文档分析方面表现优异，能够处理长篇内容并提供深入见解。',
        color: '#ec4899',
        isPro: true
      }
    ]);
    
    // 当前选择的模型（默认为deepseek-v3，因为是免费的基础模型）
    const currentModel = ref(availableModels.value.find(model => model.id === 'deepseek-v3'));
    
    // 预设问题建议
    const suggestions = ref([
      "武汉大学有哪些著名的景点？",
      "请介绍一下武汉大学的历史",
      "武汉大学的计算机专业如何？",
      "武汉大学的图书馆有什么特色？",
      "武汉大学有哪些传统活动？",
      "武汉大学的留学生项目有哪些？"
    ]);
    
    // 切换历史记录侧边栏
    const toggleHistorySidebar = () => {
      showHistorySidebar.value = !showHistorySidebar.value;
      // 关闭另一个侧边栏
      if (showHistorySidebar.value && showModelSidebar.value) {
        showModelSidebar.value = false;
      }
    };
    
    // 切换模型选择侧边栏
    const toggleModelSidebar = () => {
      showModelSidebar.value = !showModelSidebar.value;
      // 关闭另一个侧边栏
      if (showModelSidebar.value && showHistorySidebar.value) {
        showHistorySidebar.value = false;
      }
    };
    
    // 选择模型
    const selectModel = (model) => {
      // 检查模型是否可用
      if (!isModelAvailable(model)) {
        // 显示升级提示，根据模型要求的等级显示不同的提示
        if (model.id === 'gpt-3.5' && userMembershipLevel.value === 'normal') {
          showUpgradeModal.value = true;
          return;
        } else if (userMembershipLevel.value !== 'svip') {
          showUpgradeModal.value = true;
          return;
        }
      }
      
      // 设置当前模型
      currentModel.value = model;
      
      // 关闭侧边栏
      showModelSidebar.value = false;
    };
    
    // 加载历史消息
    const loadMessages = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/history/${userId.value}`);
        if (response.data && response.data.success) {
          const history = response.data.history;
          if (history && history.length > 0) {
            // 确保每条消息都有timestamp属性
            messages.value = history.map(msg => ({
              role: msg.role,
              content: msg.content,
              id: msg.id,
              timestamp: msg.timestamp ? new Date(msg.timestamp) : new Date()
            }));
            await nextTick();
            scrollToBottom();
          }
        }
      } catch (error) {
        console.error('加载历史消息失败:', error);
      }
    };
    
    // 加载聊天历史列表
    const loadChatHistoryList = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat-sessions/${userId.value}`);
        if (response.data && response.data.success) {
          chatHistory.value = response.data.sessions || [];
        }
      } catch (error) {
        console.error('加载聊天历史列表失败:', error);
      }
    };
    
    // 加载特定对话历史
    const loadChatHistory = async (chatId) => {
      try {
        const response = await axios.get(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat-session/${chatId}`);
        if (response.data && response.data.success) {
          messages.value = response.data.messages || [];
          showHistorySidebar.value = false;
          await nextTick();
          scrollToBottom();
        }
      } catch (error) {
        console.error('加载对话历史失败:', error);
      }
    };
    
    // 开始新对话
    const startNewChat = () => {
      messages.value = [];
      showHistorySidebar.value = false;
    };
    
    // 复制内容到剪贴板
    const copyToClipboard = async (text) => {
      try {
        await navigator.clipboard.writeText(text);
        showCopyToast.value = true;
        setTimeout(() => {
          showCopyToast.value = false;
        }, 2000);
      } catch (error) {
        console.error('复制内容失败:', error);
      }
    };
    
    // 重新生成回答
    const regenerateResponse = async (messageIndex) => {
      if (loading.value) return;
      
      // 找到当前回答对应的问题
      const userMessageIndex = messageIndex - 1;
      if (userMessageIndex < 0 || messages.value[userMessageIndex].role !== 'user') {
        return;
      }
      
      // 删除当前回答
      messages.value.splice(messageIndex, 1);
      
      // 设置加载状态
      loading.value = true;
      
      // 重新提问
      const userMessage = messages.value[userMessageIndex];
      
      try {
        // 创建历史消息数组（重新发送请求）
        const recentMessages = messages.value.slice(-10).map(msg => ({
          role: msg.role,
          content: msg.content
        }));
        
        // 发送请求
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat`, {
          userId: userId.value,
          username: username.value,
          message: userMessage.content,
          history: recentMessages,
          modelId: currentModel.value.id
        });
        
        // 添加AI回复
        if (response.data && response.data.success) {
          messages.value.push({
            role: 'assistant',
            content: response.data.message,
            timestamp: new Date()
          });
        } else {
          // 处理错误
          messages.value.push({
            role: 'assistant',
            content: `抱歉，我遇到了一些问题：${response.data?.error || '未知错误'}`,
            timestamp: new Date()
          });
        }
      } catch (error) {
        console.error('AI请求失败:', error);
        // 添加错误消息
        messages.value.push({
          role: 'assistant',
          content: '抱歉，我暂时无法回应你的问题。请稍后再试。',
          timestamp: new Date()
        });
      } finally {
        // 取消加载状态
        loading.value = false;
        
        // 滚动到底部
        await nextTick();
        scrollToBottom();
      }
    };
    
    // 显示高级功能（因为用户不是会员，所以显示提示）
    const showAdvancedFeatures = () => {
      showAdvancedFeatureModal.value = true;
    };
    
    // 附加文件（因为用户不是会员，所以显示提示）
    const attachFile = () => {
      showAdvancedFeatureModal.value = true;
    };
    
    // 显示会员权益提示
    const showMembershipBenefits = () => {
      showUpgradeModal.value = true;
    };
    
    // 显示升级提示
    const showUpgrade = () => {
      showAdvancedFeatureModal.value = false;
      showUpgradeModal.value = true;
    };
    
    // 使用建议问题
    const useQuestion = (question) => {
      messageText.value = question;
      if (inputField.value) {
        inputField.value.focus();
      }
    };
    
    // 发送消息
    const sendMessage = async () => {
      if (loading.value || !messageText.value.trim()) {
        return;
      }
      
      const userMessage = {
        role: 'user',
        content: messageText.value,
        timestamp: new Date()
      };
      
      // 添加用户消息
      messages.value.push(userMessage);
      
      // 清空输入框
      const userMessageText = messageText.value;
      messageText.value = '';
      
      // 滚动到底部
      await nextTick();
      scrollToBottom();
      
      // 设置加载状态
      loading.value = true;
      
      try {
        // 创建历史消息数组（只传递最近10条消息，避免过长）
        const recentMessages = messages.value.slice(-10).map(msg => ({
          role: msg.role,
          content: msg.content
        }));
        
        // 发送请求
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat`, {
          userId: userId.value,
          username: username.value,
          message: userMessageText,
          history: recentMessages,
          modelId: currentModel.value.id
        });
        
        // 添加AI回复
        if (response.data && response.data.success) {
          messages.value.push({
            role: 'assistant',
            content: response.data.message,
            timestamp: new Date()
          });
        } else {
          // 处理错误
          messages.value.push({
            role: 'assistant',
            content: `抱歉，我遇到了一些问题：${response.data?.error || '未知错误'}`,
            timestamp: new Date()
          });
        }
      } catch (error) {
        console.error('AI请求失败:', error);
        // 添加错误消息
        messages.value.push({
          role: 'assistant',
          content: '抱歉，我暂时无法回应你的问题。请稍后再试。',
          timestamp: new Date()
        });
      } finally {
        // 取消加载状态
        loading.value = false;
        
        // 滚动到底部
        await nextTick();
        scrollToBottom();
      }
    };
    
    // Ctrl+Enter添加换行
    const addNewLine = () => {
      messageText.value += '\n';
    };
    
    // 确认清空历史
    const confirmClearHistory = () => {
      showClearConfirm.value = true;
    };
    
    // 清空聊天历史
    const clearHistory = async () => {
      try {
        const response = await axios.delete(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/history/${userId.value}`);
        if (response.data && response.data.success) {
          messages.value = [];
          showClearConfirm.value = false;
        } else {
          console.error('清空历史失败:', response.data?.error);
        }
      } catch (error) {
        console.error('清空历史请求失败:', error);
      }
    };
    
    // 格式化消息（处理换行符和特殊标记）
    const formatMessage = (text) => {
      if (!text) return '';
      
      // 将换行符转换为<br>
      let formatted = text.replace(/\n/g, '<br>');
      
      // 将*text*转换为<strong>text</strong>
      formatted = formatted.replace(/\*([^*]+)\*/g, '<strong>$1</strong>');
      
      // 将_text_转换为<em>text</em>
      formatted = formatted.replace(/\_([^_]+)\_/g, '<em>$1</em>');
      
      return formatted;
    };
    
    // 格式化时间
    const formatTime = (timestamp) => {
      if (!timestamp) return '';
      
      const date = new Date(timestamp);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 格式化历史日期
    const formatHistoryDate = (timestamp) => {
      if (!timestamp) return '';
      
      const date = new Date(timestamp);
      const now = new Date();
      const isToday = date.getDate() === now.getDate() && 
                      date.getMonth() === now.getMonth() && 
                      date.getFullYear() === now.getFullYear();
      
      if (isToday) {
        return date.toLocaleTimeString('zh-CN', {
          hour: '2-digit',
          minute: '2-digit'
        });
      } else {
        return date.toLocaleDateString('zh-CN', {
          month: 'short',
          day: 'numeric'
        });
      }
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      if (chatContent.value) {
        chatContent.value.scrollTop = chatContent.value.scrollHeight;
      }
    };
    
    // 监听消息变化，滚动到底部
    watch(messages, () => {
      nextTick(() => scrollToBottom());
    });
    
    // 组件挂载时
    onMounted(() => {
      // 加载历史消息
      loadMessages();
      
      // 加载聊天历史列表
      loadChatHistoryList();
      
      // 获取用户会员信息
      fetchUserMembership();
      
      // 自动聚焦输入框
      if (inputField.value) {
        inputField.value.focus();
      }
    });
    
    return {
      messages,
      messageText,
      loading,
      chatContent,
      inputField,
      showClearConfirm,
      showUpgradeModal,
      showAdvancedFeatureModal,
      showCopyToast,
      showHistorySidebar,
      showModelSidebar,
      chatHistory,
      availableModels,
      currentModel,
      userIsPro,
      userIsSVIP,
      userMembershipLevel,
      suggestions,
      useQuestion,
      sendMessage,
      addNewLine,
      confirmClearHistory,
      clearHistory,
      formatMessage,
      formatTime,
      formatHistoryDate,
      scrollToBottom,
      username: username.value,
      toggleHistorySidebar,
      toggleModelSidebar,
      selectModel,
      loadChatHistory,
      startNewChat,
      copyToClipboard,
      regenerateResponse,
      showAdvancedFeatures,
      attachFile,
      showUpgrade,
      isModelAvailable,
      showMembershipBenefits,
      fetchUserMembership
    };
  }
};
</script>

<style scoped>
/* 全局样式 */
.chat-app {
  display: flex;
  width: 100%;
  height: 100vh;
  overflow: hidden;
  background-color: #f9fafb;
  position: relative;
}

/* 侧边栏样式 */
.sidebar {
  background-color: white;
  width: 280px;
  height: 100%;
  display: flex;
  flex-direction: column;
  border-right: 1px solid #eaeaea;
  transition: transform 0.3s ease;
  z-index: 10;
}

.sidebar-left {
  border-right: 1px solid #eaeaea;
}

.sidebar-right {
  border-left: 1px solid #eaeaea;
}

.sidebar-closed {
  transform: translateX(-100%);
}

.sidebar-right.sidebar-closed {
  transform: translateX(100%);
}

.sidebar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #eaeaea;
}

.sidebar-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.sidebar-content {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

.sidebar-footer {
  padding: 16px;
  border-top: 1px solid #eaeaea;
}

/* 历史记录样式 */
.empty-history {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #9ca3af;
  padding: 30px 0;
}

.empty-history svg {
  margin-bottom: 10px;
  stroke: #9ca3af;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.history-item {
  padding: 12px 15px;
  border-radius: 8px;
  background-color: #f9fafb;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.history-item:hover {
  background-color: #f1f5f9;
}

.history-item-title {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 4px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.history-item-time {
  font-size: 12px;
  color: #6b7280;
}

.new-chat-button {
  width: 100%;
  padding: 10px;
  background-color: #4f46e5;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  transition: background-color 0.2s ease;
}

.new-chat-button:hover {
  background-color: #4338ca;
}

/* 模型选择样式 */
.model-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.model-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.model-item:hover {
  border-color: #d1d5db;
  background-color: #f9fafb;
}

.model-selected {
  border-color: #4f46e5;
  background-color: #eef2ff;
}

.model-locked {
  cursor: not-allowed;
  opacity: 0.8;
}

.model-locked::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.4);
  border-radius: 8px;
}

.model-item-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  color: white;
  margin-right: 12px;
  flex-shrink: 0;
}

.model-item-details {
  flex: 1;
  min-width: 0;
}

.model-item-name {
  font-size: 14px;
  font-weight: 600;
  color: #333;
  display: flex;
  align-items: center;
  margin-bottom: 4px;
}

.model-item-tag {
  font-size: 10px;
  font-weight: 500;
  color: white;
  padding: 2px 6px;
  border-radius: 12px;
  margin-left: 6px;
}

.model-item-desc {
  font-size: 12px;
  color: #6b7280;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.model-item-status {
  display: flex;
  align-items: center;
  margin-left: 6px;
}

.model-item-status svg {
  stroke: #4f46e5;
}

.upgrade-card {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  border-radius: 8px;
  padding: 16px;
  color: white;
}

.upgrade-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 12px;
}

.upgrade-button {
  width: 100%;
  padding: 8px 12px;
  background-color: white;
  color: #4f46e5;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.upgrade-button:hover {
  background-color: #f8fafc;
}

/* 主内容区域样式 */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
  position: relative;
}

/* 聊天头部样式 */
.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background-color: white;
  border-bottom: 1px solid #eaeaea;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  z-index: 5;
}

.header-left, .header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.header-right {
  justify-content: flex-end;
}

.header-title {
  display: flex;
  align-items: center;
  justify-content: center;
}

.current-model-info {
  display: flex;
  align-items: center;
}

.model-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 12px;
}

.model-details {
  display: flex;
  flex-direction: column;
}

.model-details h1 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.model-tag {
  font-size: 10px;
  color: #4f46e5;
  background-color: #eef2ff;
  border: 1px solid #c7d2fe;
  padding: 2px 8px;
  border-radius: 12px;
  margin-top: 4px;
  display: inline-block;
}

.pro-tag {
  color: white;
  background-color: #f43f5e;
  border-color: #f43f5e;
}

.back-button {
  display: flex;
  align-items: center;
  color: #6366f1;
  text-decoration: none;
  font-size: 14px;
  gap: 6px;
}

.icon-button {
  background: none;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  color: #6b7280;
  transition: background-color 0.2s ease;
}

.icon-button:hover {
  background-color: #f3f4f6;
}

.clear-button {
  display: flex;
  align-items: center;
  background: transparent;
  border: none;
  color: #f43f5e;
  cursor: pointer;
  font-size: 14px;
  padding: 6px 12px;
  border-radius: 6px;
  transition: background-color 0.2s ease;
  gap: 6px;
}

.clear-button:hover {
  background-color: #fee2e2;
}

/* 聊天内容区域样式 */
.chat-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  overflow-x: hidden;
}

.welcome-container {
  max-width: 720px;
  margin: 0 auto 30px;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  padding: 30px;
  animation: fadeIn 0.5s ease;
}

.welcome-header {
  display: flex;
  align-items: center;
  margin-bottom: 20px;
}

.welcome-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 16px;
  font-size: 18px;
}

.welcome-header h2 {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
  color: #333;
}

.welcome-message {
  margin-bottom: 24px;
  color: #555;
  font-size: 15px;
  line-height: 1.6;
}

.model-description {
  background-color: #f9fafb;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 24px;
}

.model-description h3 {
  margin: 0 0 12px 0;
  font-size: 16px;
  font-weight: 600;
  color: #444;
}

.model-description p {
  margin: 0 0 16px 0;
  font-size: 14px;
  color: #555;
  line-height: 1.6;
}

.upgrade-prompt {
  background-color: #eef2ff;
  border-radius: 8px;
  padding: 12px 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.upgrade-prompt p {
  margin: 0;
  font-size: 13px;
  color: #4f46e5;
  font-weight: 500;
}

.upgrade-prompt .upgrade-button {
  background-color: #4f46e5;
  color: white;
  padding: 6px 12px;
  font-size: 13px;
}

.upgrade-prompt .upgrade-button:hover {
  background-color: #4338ca;
}

.suggestions {
  background-color: #f9fafb;
  border-radius: 8px;
  padding: 20px;
}

.suggestion-title {
  font-size: 15px;
  font-weight: 600;
  color: #444;
  margin-bottom: 16px;
}

.suggestion-items {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-gap: 12px;
}

.suggestion-item {
  background-color: white;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 14px;
  color: #4b5563;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 8px;
}

.suggestion-item:hover {
  background-color: #eef2ff;
  border-color: #c7d2fe;
  color: #4f46e5;
}

.suggestion-item svg {
  stroke: #6366f1;
  flex-shrink: 0;
}

.message-container {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  padding: 16px;
  margin-bottom: 20px;
  max-width: 80%;
  animation: fadeIn 0.3s ease;
  position: relative;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.user-container {
  background-color: #eef2ff;
  margin-left: auto;
}

.ai-container {
  background-color: white;
  margin-right: auto;
}

.message-header {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}

.message-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 12px;
  flex-shrink: 0;
}

.user-avatar {
  background: linear-gradient(135deg, #1677ff, #69c0ff);
}

.message-info {
  display: flex;
  flex-direction: column;
}

.message-name {
  font-size: 14px;
  font-weight: 600;
  color: #333;
}

.message-time {
  font-size: 12px;
  color: #9ca3af;
  margin-top: 2px;
}

.message-body {
  font-size: 15px;
  line-height: 1.6;
  color: #333;
  white-space: pre-line;
}

.message-actions {
  position: absolute;
  right: 16px;
  top: 12px;
  display: flex;
  gap: 8px;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.message-container:hover .message-actions {
  opacity: 1;
}

.action-button {
  background: none;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border-radius: 4px;
  color: #6b7280;
  transition: background-color 0.2s ease;
}

.action-button:hover {
  background-color: #f3f4f6;
  color: #4b5563;
}

.typing-container {
  display: flex;
  align-items: flex-start;
  margin-bottom: 20px;
  max-width: 80%;
}

.typing-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 12px;
  flex-shrink: 0;
}

.typing-content {
  background-color: white;
  border-radius: 12px;
  padding: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  min-width: 120px;
}

.typing-name {
  font-size: 14px;
  font-weight: 600;
  color: #333;
  margin-bottom: 12px;
}

.typing-indicator {
  display: flex;
}

.typing-indicator span {
  width: 8px;
  height: 8px;
  margin: 0 2px;
  background-color: #6366f1;
  border-radius: 50%;
  display: inline-block;
  animation: blink 1.4s infinite both;
}

.typing-indicator span:nth-child(2) {
  animation-delay: 0.2s;
}

.typing-indicator span:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes blink {
  0% {
    opacity: 0.2;
  }
  20% {
    opacity: 1;
  }
  100% {
    opacity: 0.2;
  }
}

/* 输入区域样式 */
.chat-input {
  padding: 16px 24px 24px;
  background-color: white;
  border-top: 1px solid #eaeaea;
}

.chat-input-container {
  display: flex;
  position: relative;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  transition: border-color 0.3s ease;
  background-color: white;
}

.chat-input-container:focus-within {
  border-color: #6366f1;
  box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.1);
}

textarea {
  flex: 1;
  min-height: 24px;
  max-height: 200px;
  border: none;
  border-radius: 8px;
  padding: 14px 60px 14px 16px;
  font-size: 15px;
  resize: none;
  outline: none;
  background: transparent;
}

.input-actions {
  position: absolute;
  right: 60px;
  bottom: 10px;
  display: flex;
  gap: 8px;
}

.feature-button {
  background: none;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  color: #9ca3af;
  transition: all 0.2s ease;
}

.feature-button:hover {
  color: #6366f1;
  background-color: #f3f4f6;
}

.send-button {
  position: absolute;
  right: 8px;
  bottom: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #6366f1;
  color: white;
  border: none;
  border-radius: 6px;
  width: 36px;
  height: 36px;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.send-button:hover {
  background-color: #4f46e5;
}

.send-button:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
}

.input-features {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
  padding: 0 8px;
  font-size: 12px;
  color: #6b7280;
}

.model-info {
  display: flex;
  align-items: center;
  gap: 6px;
}

.model-change {
  color: #6366f1;
  cursor: pointer;
  font-weight: 500;
}

.model-change:hover {
  text-decoration: underline;
}

/* 模态框样式 */
.modal-overlay {
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
  animation: fadeIn 0.3s ease;
}

.modal-content {
  background-color: white;
  border-radius: 12px;
  width: 420px;
  max-width: 90%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  overflow: hidden;
  animation: scaleIn 0.3s ease;
}

@keyframes scaleIn {
  from {
    opacity: 0;
    transform: scale(0.95);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

.modal-header {
  padding: 16px 20px;
  border-bottom: 1px solid #eaeaea;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
  display: flex;
  align-items: center;
  gap: 8px;
}

.modal-body {
  padding: 20px;
}

.modal-body p {
  margin: 0;
  font-size: 14px;
  color: #666;
  line-height: 1.5;
}

.modal-footer {
  padding: 16px 20px;
  border-top: 1px solid #eaeaea;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.modal-cancel, .modal-confirm {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
}

.modal-cancel {
  background-color: #f9fafb;
  border: 1px solid #d1d5db;
  color: #6b7280;
}

.modal-confirm {
  background-color: #f43f5e;
  border: none;
  color: white;
}

.modal-upgrade {
  width: 600px;
  max-width: 90%;
}

.upgrade-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.upgrade-illustration {
  margin-bottom: 16px;
}

.upgrade-illustration svg {
  stroke: #4f46e5;
}

.upgrade-content h4 {
  margin: 0 0 20px 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.upgrade-features {
  list-style: none;
  padding: 0;
  margin: 0 0 20px 0;
  width: 100%;
  text-align: left;
}

.upgrade-features li {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 0;
  font-size: 14px;
  color: #4b5563;
}

.upgrade-features li svg {
  stroke: #10b981;
  flex-shrink: 0;
}

.upgrade-notice {
  font-size: 13px;
  color: #6b7280;
  margin: 0;
  padding-top: 16px;
  border-top: 1px dashed #e5e7eb;
}

.modal-upgrade-btn {
  background-color: #6366f1;
}

.modal-upgrade-btn:disabled {
  background-color: #a5b4fc;
  cursor: not-allowed;
}

/* 复制成功提示 */
.copy-toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background-color: #4b5563;
  color: white;
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  animation: fadeInUp 0.3s ease;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translate(-50%, 20px);
  }
  to {
    opacity: 1;
    transform: translate(-50%, 0);
  }
}

.copy-toast svg {
  stroke: #10b981;
}

/* 响应式设计 */
@media (max-width: 992px) {
  .sidebar {
    position: absolute;
    top: 0;
    height: 100%;
    z-index: 20;
  }
  
  .sidebar-left {
    left: 0;
  }
  
  .sidebar-right {
    right: 0;
  }
  
  .message-container {
    max-width: 90%;
  }
}

@media (max-width: 768px) {
  .suggestion-items {
    grid-template-columns: 1fr;
  }
  
  .header-title h1 {
    font-size: 16px;
  }
  
  .model-tag {
    display: none;
  }
}

@media (max-width: 576px) {
  .chat-header {
    padding: 12px 16px;
  }
  
  .back-button span {
    display: none;
  }
  
  .clear-button span {
    display: none;
  }
  
  .model-avatar {
    margin-right: 8px;
  }
  
  .chat-content {
    padding: 16px 12px;
  }
  
  .welcome-container {
    padding: 20px 16px;
  }
  
  .message-container {
    max-width: 95%;
    padding: 12px;
  }
}

/* 会员状态显示 */
.membership-status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  padding: 12px;
  border-radius: 8px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
}

.membership-badge {
  font-size: 14px;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 12px;
}

.level-normal {
  background-color: #e5e7eb;
  color: #6b7280;
}

.level-vip {
  background-color: #8b5cf6;
  color: white;
}

.level-svip {
  background: linear-gradient(135deg, #f59e0b, #ef4444);
  color: white;
}

.upgrade-link {
  font-size: 13px;
  font-weight: 500;
  color: #6366f1;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
}

.upgrade-link:hover {
  text-decoration: underline;
}

/* 会员标签样式 */
.model-item-tag {
  font-size: 10px;
  font-weight: 500;
  color: white;
  padding: 2px 6px;
  border-radius: 12px;
  margin-left: 6px;
}

.vip-tag {
  background-color: #8b5cf6;
}

.svip-tag {
  background: linear-gradient(135deg, #f59e0b, #ef4444);
}

/* 会员计划样式 */
.membership-plans {
  display: flex;
  gap: 16px;
  margin: 20px 0;
}

.membership-plan {
  flex: 1;
  border: 1px solid #e5e7eb;
  border-radius: 10px;
  padding: 16px;
  position: relative;
  transition: all 0.3s ease;
}

.membership-plan:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  transform: translateY(-2px);
}

.current-plan {
  border-color: #8b5cf6;
  background-color: #f5f3ff;
}

.plan-tag {
  position: absolute;
  top: -10px;
  right: 16px;
  background: linear-gradient(135deg, #f59e0b, #ef4444);
  color: white;
  font-size: 12px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 12px;
}

.plan-name {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #333;
}

.plan-price {
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 16px;
  color: #4f46e5;
}

.plan-features {
  list-style: none;
  padding: 0;
  margin: 0 0 20px 0;
}

.plan-features li {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  font-size: 14px;
  color: #4b5563;
}

.plan-features li svg {
  stroke: #10b981;
  flex-shrink: 0;
}

.plan-button {
  width: 100%;
  padding: 8px 0;
  border: 1px solid #6366f1;
  background-color: white;
  color: #6366f1;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.plan-button:hover {
  background-color: #eef2ff;
}

.premium-button {
  background-color: #6366f1;
  color: white;
}

.premium-button:hover {
  background-color: #4f46e5;
}
</style> 