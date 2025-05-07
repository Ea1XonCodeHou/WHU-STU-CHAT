<template>
  <div class="post-detail">
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner">
        <i class="fas fa-spinner fa-spin"></i>
      </div>
      <p>加载中...</p>
    </div>
    
    <div v-else-if="error" class="error-container">
      <i class="fas fa-exclamation-circle"></i>
      <p>{{ error }}</p>
      <button @click="loadPost" class="retry-button" style="display: none;">重试</button>
    </div>
    
    <template v-else-if="post">
      <div class="post-header">
        <div class="post-title-container">
          <h1 class="post-title">{{ post.title }}</h1>
          <div class="post-badges">
            <span v-if="post.postType === 'sticky'" class="post-badge sticky">
              <i class="fas fa-thumbtack"></i> 置顶
            </span>
          </div>
        </div>
        
        <div class="post-meta">
          <div class="post-author">
            <div class="author-avatar">
              <img v-if="author && author.avatar" :src="author.avatar" alt="author avatar">
              <div v-else class="default-avatar">{{ getInitials(post.isAnonymous ? '匿名' : (author ? author.username : '')) }}</div>
            </div>
            <div class="author-info">
              <div class="author-name">{{ post.isAnonymous ? '匿名用户' : (author ? author.username : '未知用户') }}</div>
              <div class="post-time">{{ formatDate(post.createTime) }}</div>
            </div>
          </div>
          
          <div class="post-actions" v-if="isCurrentUserAuthor">
            <button @click="showEditModal = true" class="action-button edit">
              <i class="fas fa-edit"></i>
            </button>
            <button @click="confirmDelete" class="action-button delete">
              <i class="fas fa-trash"></i>
            </button>
          </div>
        </div>
      </div>
      
      <div class="post-content">
        {{ post.content }}
      </div>
      
      <div class="post-stats">
        <div class="post-stat">
          <i class="fas fa-thumbs-up" 
             :class="{ 'liked': hasLiked }" 
             @click="likePost"></i>
          <span>{{ post.likeCount }} 点赞</span>
        </div>
        <div class="post-stat">
          <i class="fas fa-comment"></i>
          <span>{{ post.commentCount }} 评论</span>
        </div>
        <div class="post-stat">
          <i class="fas fa-clock"></i>
          <span>{{ formatDate(post.updateTime) }}</span>
        </div>
      </div>
      
      <div class="divider"></div>
      
      <div class="comment-section">
        <h3 class="section-title">
          <i class="fas fa-comments"></i>
          评论 ({{ comments.length }})
        </h3>
        
        <div class="comment-form">
          <div class="comment-input-container">
            <div class="comment-avatar">
              <div class="default-avatar">{{ getInitials(currentUsername) }}</div>
            </div>
            <div class="comment-input">
              <textarea 
                v-model="newComment" 
                placeholder="添加评论..." 
                rows="3"
              ></textarea>
            </div>
          </div>
          <div class="comment-actions">
            <span class="reply-to" v-if="replyToId">
              回复 #{{ replyToId }} 
              <button @click="cancelReply" class="cancel-reply">
                <i class="fas fa-times"></i>
              </button>
            </span>
            <div class="checkbox-container">
              <input 
                id="commentAnonymous" 
                type="checkbox" 
                v-model="isCommentAnonymous"
              />
              <label for="commentAnonymous">匿名评论</label>
            </div>
            <button 
              @click="submitComment" 
              class="comment-submit" 
              :disabled="commentLoading || !newComment.trim()"
            >
              <span v-if="commentLoading">
                <i class="fas fa-spinner fa-spin"></i> 提交中...
              </span>
              <span v-else>
                <i class="fas fa-paper-plane"></i> 发表评论
              </span>
            </button>
          </div>
        </div>
        
        <div class="comments-container">
          <div v-if="comments.length === 0" class="empty-comments">
            <i class="fas fa-comment-slash"></i>
            <p>暂无评论，快来发表第一个评论吧！</p>
          </div>
          
          <div v-else class="comment-items">
            <div 
              v-for="comment in sortedComments" 
              :key="comment.commentId"
              class="comment-item"
              :class="{ 'is-reply': comment.parentId > 0 }"
              :style="comment.parentId > 0 ? { marginLeft: '30px' } : {}"
            >
              <div class="comment-header">
                <div class="comment-author">
                  <div class="author-avatar">
                    <div class="default-avatar">{{ getInitials(comment.isAnonymous ? '匿名' : (comment.username || '')) }}</div>
                  </div>
                  <div class="author-info">
                    <div class="author-name">{{ comment.isAnonymous ? '匿名用户' : comment.username }}</div>
                    <div class="comment-time">{{ formatDate(comment.createTime) }}</div>
                  </div>
                </div>
                
                <div class="comment-actions" v-if="currentUserId === comment.userId">
                  <button @click="deleteComment(comment.commentId)" class="action-button delete">
                    <i class="fas fa-trash"></i>
                  </button>
                </div>
              </div>
              
              <div class="comment-content">
                <div v-if="comment.parentId > 0" class="reply-indicator">
                  回复 #{{ comment.parentId }}
                </div>
                {{ comment.content }}
              </div>
              
              <div class="comment-footer">
                <button @click="replyTo(comment.commentId)" class="action-button reply">
                  <i class="fas fa-reply"></i> 回复
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
    
    <!-- 编辑帖子模态窗口 -->
    <div v-if="showEditModal" class="modal-overlay" @click="showEditModal = false">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <h3>编辑帖子</h3>
          <button @click="showEditModal = false" class="close-button">
            <i class="fas fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="updatePost">
            <div class="form-group">
              <label for="edit-title">标题</label>
              <input 
                id="edit-title" 
                type="text" 
                v-model="editedPost.title" 
                placeholder="帖子标题"
                required
              />
            </div>
            <div class="form-group">
              <label for="edit-content">内容</label>
              <textarea 
                id="edit-content" 
                v-model="editedPost.content" 
                placeholder="帖子内容"
                required
              ></textarea>
            </div>
            <div class="form-group checkbox">
              <input 
                id="edit-sticky" 
                type="checkbox" 
                v-model="editedPost.isSticky"
              />
              <label for="edit-sticky">置顶帖子</label>
            </div>
            <div class="form-group checkbox">
              <input 
                id="edit-anonymous" 
                type="checkbox" 
                v-model="editedPost.isAnonymous"
              />
              <label for="edit-anonymous">匿名发布</label>
            </div>
            <div class="form-actions">
              <button type="button" class="cancel-button" @click="showEditModal = false">取消</button>
              <button type="submit" class="submit-button" :disabled="editLoading">
                <span v-if="editLoading">
                  <i class="fas fa-spinner fa-spin"></i> 更新中...
                </span>
                <span v-else>更新帖子</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, watch } from 'vue';
import axios from 'axios';
import { ElMessage } from 'element-plus';

export default {
  name: 'PostDetail',
  props: {
    postId: {
      type: Number,
      required: true
    }
  },
  emits: ['close'],
  setup(props, { emit }) {
    // 状态变量
    const post = ref(null);
    const loading = ref(true);
    const error = ref(null);
    const comments = ref([]);
    const loadingComments = ref(false);
    const currentUserId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const currentUsername = ref(localStorage.getItem('username') || '未知用户');
    const author = ref(null);
    const isCurrentUserAuthor = computed(() => post.value && post.value.authorId === currentUserId.value);
    const hasLiked = ref(false);
    
    // 评论相关
    const newComment = ref('');
    const replyToId = ref(0);
    const commentLoading = ref(false);
    const isCommentAnonymous = ref(false);
    
    // 编辑相关
    const showEditModal = ref(false);
    const editedPost = ref({
      title: '',
      content: '',
      isSticky: false,
      isAnonymous: false
    });
    const editLoading = ref(false);
    
    // 评论按创建时间排序，回复跟随原评论
    const sortedComments = computed(() => {
      const result = [];
      const mainComments = comments.value.filter(c => c.parentId === 0);
      
      // 按时间排序主评论
      mainComments.sort((a, b) => new Date(a.createTime) - new Date(b.createTime));
      
      // 对每个主评论，找到它的所有回复
      mainComments.forEach(main => {
        result.push(main);
        const replies = comments.value.filter(c => c.parentId === main.commentId);
        replies.sort((a, b) => new Date(a.createTime) - new Date(b.createTime));
        result.push(...replies);
      });
      
      return result;
    });
    
    // 加载帖子详情
    const loadPost = async () => {
      loading.value = true;
      error.value = null;
      
      try {
        const response = await axios.get(`/api/discussion/posts/${props.postId}`);
        post.value = response.data;
        editedPost.value = {
          title: post.value.title,
          content: post.value.content,
          isSticky: post.value.postType === 'sticky',
          isAnonymous: post.value.isAnonymous
        };
        
        // 加载评论
        await loadComments();
        
        // 检查点赞状态
        await checkLikeStatus();
        
        // 获取作者信息
        try {
          // 如果是匿名发布，则显示"匿名用户"
          if (post.value.isAnonymous) {
            author.value = { username: '匿名用户' };
          } else {
            // 非匿名，尝试获取用户信息，如果获取失败则直接从帖子中获取
            try {
              const userResponse = await axios.get(`/api/discussion/posts/${props.postId}/author`);
              if (userResponse.data && userResponse.data.username) {
                author.value = userResponse.data;
              } else {
                // 如果无法获取作者详细信息，则使用默认值
                console.log('作者信息不完整，使用默认值');
                author.value = { 
                  username: `用户${post.value.authorId}`,
                  id: post.value.authorId
                };
              }
            } catch (error) {
              console.log(`获取作者信息失败：${error.message}`);
              author.value = { 
                username: `用户${post.value.authorId}`,
                id: post.value.authorId 
              };
            }
          }
        } catch (userError) {
          console.error('获取作者信息失败:', userError);
          author.value = { username: '未知用户' };
        }
      } catch (err) {
        console.error('加载帖子失败:', err);
        error.value = '加载帖子失败: ' + (err.response?.data || err.message);
      } finally {
        loading.value = false;
      }
    };
    
    // 加载评论
    const loadComments = async () => {
      loadingComments.value = true;
      
      try {
        const response = await axios.get(`/api/discussion/posts/${props.postId}/comments`);
        comments.value = response.data;
        
        // 获取评论者信息
        for (const comment of comments.value) {
          try {
            // 如果是匿名评论，显示"匿名用户"
            if (comment.isAnonymous) {
              comment.username = '匿名用户';
            } else {
              // 尝试获取评论者用户信息
              try {
                const userResponse = await axios.get(`/api/discussion/comments/${comment.commentId}/author`);
                if (userResponse.data && userResponse.data.username) {
                  comment.username = userResponse.data.username;
                } else {
                  // 如果无法获取评论者详细信息，则使用默认值
                  comment.username = `用户${comment.userId}`;
                }
              } catch (error) {
                console.log(`获取评论者信息失败：${error.message}`);
                comment.username = `用户${comment.userId}`;
              }
            }
          } catch (userError) {
            console.error(`获取评论者(ID:${comment.userId})信息失败:`, userError);
            comment.username = `用户${comment.userId}`;
          }
        }
      } catch (err) {
        console.error('加载评论失败:', err);
        ElMessage.error('加载评论失败: ' + (err.response?.data || err.message));
      } finally {
        loadingComments.value = false;
      }
    };
    
    // 提交评论
    const submitComment = async () => {
      if (!newComment.value.trim()) return;
      
      commentLoading.value = true;
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          alert('请先登录');
          return;
        }
        
        const commentData = {
          postId: post.value.postId,
          content: newComment.value,
          userId: parseInt(userId),
          parentId: replyToId.value,
          isAnonymous: isCommentAnonymous.value
        };
        
        const response = await axios.post(`/api/discussion/posts/${post.value.postId}/comments`, commentData);
        console.log('评论发布成功:', response.data);
        
        // 重置表单
        newComment.value = '';
        replyToId.value = 0;
        isCommentAnonymous.value = false;
        
        // 重新获取评论列表
        await loadComments();
        
        // 更新帖子数据（评论数可能有变化）
        const postResponse = await axios.get(`/api/discussion/posts/${props.postId}`);
        post.value = postResponse.data;
      } catch (error) {
        console.error('发表评论失败:', error);
        alert('发表评论失败: ' + (error.response?.data || error.message));
      } finally {
        commentLoading.value = false;
      }
    };
    
    // 回复评论
    const replyTo = (commentId) => {
      replyToId.value = commentId;
      // 滚动到评论输入框
      setTimeout(() => {
        document.querySelector('.comment-form textarea').focus();
      }, 100);
    };
    
    // 取消回复
    const cancelReply = () => {
      replyToId.value = 0;
    };
    
    // 删除评论
    const deleteComment = async (commentId) => {
      if (!confirm('确定要删除此评论吗？')) {
        return;
      }
      
      try {
        await axios.delete(`/api/discussion/comments/${commentId}`);
        ElMessage.success('评论删除成功');
        
        // 重新加载评论
        await loadComments();
        
        // 更新帖子数据（评论数可能有变化）
        const postResponse = await axios.get(`/api/discussion/posts/${props.postId}`);
        post.value = postResponse.data;
      } catch (err) {
        console.error('删除评论失败:', err);
        ElMessage.error('删除评论失败: ' + (err.response?.data || err.message));
      }
    };
    
    // 更新帖子
    const updatePost = async () => {
      if (!editedPost.value.title || !editedPost.value.content) {
        ElMessage.warning('标题和内容不能为空');
        return;
      }
      
      editLoading.value = true;
      try {
        const updatedPost = {
          postId: post.value.postId,
          discussionId: post.value.discussionId,
          title: editedPost.value.title,
          content: editedPost.value.content,
          postType: editedPost.value.isSticky ? 'sticky' : 'normal',
          isAnonymous: editedPost.value.isAnonymous
        };
        
        await axios.put(`/api/discussion/posts/${props.postId}`, updatedPost);
        ElMessage.success('帖子更新成功');
        
        // 重新加载帖子
        await loadPost();
        
        // 关闭编辑模态窗口
        showEditModal.value = false;
      } catch (err) {
        console.error('更新帖子失败:', err);
        ElMessage.error('更新帖子失败: ' + (err.response?.data || err.message));
      } finally {
        editLoading.value = false;
      }
    };
    
    // 确认删除帖子
    const confirmDelete = () => {
      if (confirm('确定要删除此帖子吗？此操作不可恢复！')) {
        deletePost();
      }
    };
    
    // 删除帖子
    const deletePost = async () => {
      try {
        await axios.delete(`/api/discussion/posts/${props.postId}`);
        ElMessage.success('帖子删除成功');
        
        // 关闭帖子详情
        emit('close');
      } catch (err) {
        console.error('删除帖子失败:', err);
        ElMessage.error('删除帖子失败: ' + (err.response?.data || err.message));
      }
    };
    
    // 格式化日期
    const formatDate = (dateStr) => {
      if (!dateStr) return '';
      const date = new Date(dateStr);
      const now = new Date();
      
      const diffMs = now - date;
      const diffMins = Math.floor(diffMs / (1000 * 60));
      const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
      const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
      
      if (diffMins < 1) return '刚刚';
      if (diffMins < 60) return `${diffMins}分钟前`;
      if (diffHours < 24) return `${diffHours}小时前`;
      if (diffDays < 30) return `${diffDays}天前`;
      
      return date.toLocaleDateString();
    };
    
    // 获取用户名首字母作为头像
    const getInitials = (name) => {
      if (!name) return '?';
      return name.charAt(0).toUpperCase();
    };
    
    // 点赞帖子
    const likePost = async () => {
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          alert('请先登录');
          return;
        }

        if (hasLiked.value) {
          // 已点赞，则取消点赞
          await axios.delete(`/api/discussion/posts/${props.postId}/like?userId=${userId}`);
          hasLiked.value = false;
          post.value.likeCount = Math.max(0, post.value.likeCount - 1);
        } else {
          // 未点赞，则添加点赞
          await axios.post(`/api/discussion/posts/${props.postId}/like?userId=${userId}`);
          hasLiked.value = true;
          post.value.likeCount += 1;
        }
      } catch (error) {
        console.error('点赞操作失败:', error);
      }
    };
    
    // 检查是否已点赞
    const checkLikeStatus = async () => {
      try {
        const userId = localStorage.getItem('userId');
        if (!userId || !post.value) {
          return;
        }
        
        const response = await axios.get(`/api/discussion/posts/${props.postId}/like?userId=${userId}`);
        hasLiked.value = response.data.hasLiked;
      } catch (error) {
        console.error('检查点赞状态失败:', error);
      }
    };
    
    // 当帖子ID变化时，重新加载帖子
    watch(() => props.postId, (newVal, oldVal) => {
      if (newVal !== oldVal) {
        loadPost();
      }
    });
    
    // 在帖子加载成功后检查点赞状态
    watch(post, (newPost) => {
      if (newPost) {
        checkLikeStatus();
      }
    });
    
    // 组件挂载时加载帖子
    onMounted(() => {
      loadPost();
    });
    
    return {
      post,
      loading,
      error,
      comments,
      sortedComments,
      currentUserId,
      currentUsername,
      author,
      isCurrentUserAuthor,
      hasLiked,
      newComment,
      replyToId,
      commentLoading,
      isCommentAnonymous,
      showEditModal,
      editedPost,
      editLoading,
      loadPost,
      loadComments,
      submitComment,
      replyTo,
      cancelReply,
      deleteComment,
      updatePost,
      confirmDelete,
      formatDate,
      getInitials,
      likePost
    };
  }
};
</script>

<style scoped>
.post-detail {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 20px;
  overflow-y: auto;
}

.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #666;
}

.loading-spinner {
  font-size: 40px;
  color: #4776E6;
  margin-bottom: 10px;
}

.error-container i {
  font-size: 40px;
  color: #f44336;
  margin-bottom: 10px;
}

.retry-button {
  margin-top: 20px;
  padding: 8px 16px;
  background-color: #4776E6;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-button:hover {
  background-color: #3b5dc4;
}

.post-header {
  margin-bottom: 20px;
}

.post-title-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.post-title {
  font-size: 24px;
  color: #333;
  margin: 0;
  line-height: 1.3;
}

.post-badges {
  display: flex;
  gap: 10px;
}

.post-badge {
  display: inline-flex;
  align-items: center;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
}

.post-badge.sticky {
  background-color: #fff3cd;
  color: #856404;
  border: 1px solid #ffeeba;
}

.post-badge i {
  margin-right: 5px;
}

.post-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.post-author {
  display: flex;
  align-items: center;
}

.author-avatar {
  margin-right: 10px;
}

.author-avatar img,
.default-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  font-weight: bold;
  font-size: 16px;
}

.author-info {
  display: flex;
  flex-direction: column;
}

.author-name {
  font-weight: 600;
  color: #333;
  margin-bottom: 2px;
}

.post-time {
  font-size: 12px;
  color: #999;
}

.post-actions {
  display: flex;
  gap: 10px;
}

.action-button {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  background-color: #f5f5f5;
}

.action-button.edit {
  color: #4776E6;
}

.action-button.delete {
  color: #f44336;
}

.action-button.reply {
  background: none;
  color: #4776E6;
  font-size: 12px;
  width: auto;
  height: auto;
  padding: 0;
}

.action-button:hover {
  background-color: #e0e0e0;
}

.action-button.reply:hover {
  background: none;
  text-decoration: underline;
}

.post-content {
  font-size: 16px;
  line-height: 1.6;
  color: #333;
  margin-bottom: 30px;
  white-space: pre-wrap;
}

.post-stats {
  display: flex;
  gap: 20px;
  margin-bottom: 20px;
}

.post-stat {
  display: flex;
  align-items: center;
  color: #666;
  font-size: 14px;
}

.post-stat i {
  margin-right: 5px;
  color: #4776E6;
  cursor: pointer;
  transition: all 0.2s ease;
}

.post-stat i:hover {
  transform: scale(1.2);
}

.post-stat i.liked {
  color: #f44336;
}

.divider {
  height: 1px;
  background-color: #eaeaea;
  margin: 20px 0;
}

.comment-section {
  margin-top: 20px;
}

.section-title {
  font-size: 18px;
  color: #333;
  margin-bottom: 20px;
  display: flex;
  align-items: center;
}

.section-title i {
  margin-right: 8px;
  color: #4776E6;
}

.comment-form {
  background-color: #f8f9fa;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 20px;
}

.comment-input-container {
  display: flex;
}

.comment-avatar {
  margin-right: 10px;
}

.comment-avatar .default-avatar {
  width: 32px;
  height: 32px;
  font-size: 14px;
}

.comment-input {
  flex: 1;
}

.comment-input textarea {
  width: 100%;
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 10px;
  font-size: 14px;
  resize: vertical;
  outline: none;
  transition: border-color 0.2s;
}

.comment-input textarea:focus {
  border-color: #4776E6;
}

.comment-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 10px;
  padding-left: 42px;
}

.reply-to {
  font-size: 12px;
  color: #666;
  background-color: #eee;
  padding: 2px 8px;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
}

.cancel-reply {
  background: none;
  border: none;
  color: #999;
  cursor: pointer;
  margin-left: 5px;
}

.comment-submit {
  display: inline-flex;
  align-items: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 6px 14px;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.comment-submit:hover {
  box-shadow: 0 2px 8px rgba(71, 118, 230, 0.3);
}

.comment-submit:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.comment-submit i {
  margin-right: 5px;
}

.comments-container {
  margin-top: 30px;
}

.empty-comments {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 30px;
  color: #999;
  text-align: center;
}

.empty-comments i {
  font-size: 40px;
  margin-bottom: 10px;
  color: #ddd;
}

.comment-items {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.comment-item {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 15px;
}

.comment-item.is-reply {
  background-color: #f0f5ff;
  border-left: 3px solid #4776E6;
}

.comment-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.comment-author {
  display: flex;
  align-items: center;
}

.comment-author .default-avatar {
  width: 30px;
  height: 30px;
  font-size: 12px;
}

.comment-time {
  font-size: 12px;
  color: #999;
}

.comment-content {
  font-size: 14px;
  line-height: 1.5;
  color: #333;
  margin-bottom: 10px;
  word-break: break-word;
  white-space: pre-wrap;
}

.reply-indicator {
  font-size: 12px;
  color: #4776E6;
  background-color: rgba(71, 118, 230, 0.1);
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-bottom: 5px;
}

.comment-footer {
  display: flex;
  justify-content: flex-end;
}

/* 模态窗口样式 */
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
}

.modal-container {
  width: 90%;
  max-width: 600px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 5px 20px rgba(0, 0, 0, 0.2);
  overflow: hidden;
  animation: modalFadeIn 0.3s ease;
}

@keyframes modalFadeIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-bottom: 1px solid #eaeaea;
}

.modal-header h3 {
  margin: 0;
  color: #333;
  font-size: 18px;
}

.close-button {
  background: none;
  border: none;
  font-size: 16px;
  color: #666;
  cursor: pointer;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #555;
}

.form-group input[type="text"],
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input[type="text"]:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  outline: none;
}

.form-group textarea {
  height: 200px;
  resize: vertical;
}

.form-group.checkbox {
  display: flex;
  align-items: center;
}

.form-group.checkbox input {
  margin-right: 8px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.cancel-button,
.submit-button {
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-button {
  background-color: #f0f0f0;
  color: #333;
}

.submit-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.cancel-button:hover {
  background-color: #e0e0e0;
}

.submit-button:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.submit-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.checkbox-container {
  display: flex;
  align-items: center;
  margin-right: 10px;
}

.checkbox-container input {
  margin-right: 5px;
}
</style>